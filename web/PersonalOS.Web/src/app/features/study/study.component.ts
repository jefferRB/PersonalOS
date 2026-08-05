import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Observable, finalize, forkJoin, take } from 'rxjs';

import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  optionalTrimmedLength,
  parseInteger,
  requiredInteger,
  safeExternalUrl,
  trimToNull,
  trimValue,
  trimmedLength,
} from '../../core/forms/validators';
import {
  STUDY_PROJECT_STATUSES,
  STUDY_RESOURCE_TYPES,
  StudyProject,
  StudyProjectStatus,
  StudyResourceType,
  StudySession,
} from '../../core/study/study.models';
import { StudyService } from '../../core/study/study.service';
import {
  IsoLocalDate,
  addDays,
  formatMinutes,
  formatShortDate,
  startOfWeek,
  toIsoLocalDate,
  weekDays,
} from '../../core/time/local-date';
import { TodayService } from '../../core/today/today.service';

/** One column of the weekly layout. */
interface StudyDay {
  readonly date: IsoLocalDate;
  readonly label: string;
  readonly weekdayName: string;
  readonly sessions: readonly StudySession[];
  readonly minutes: number;
}

/** One resource row inside the project form. */
type ResourceGroupControls = {
  title: FormControl<string>;
  resourceType: FormControl<StudyResourceType>;
  externalUrl: FormControl<string>;
  notes: FormControl<string>;
};

const WEEKDAY_NAMES = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

/**
 * Study projects, their material, and the week's sessions.
 *
 * Resources are metadata only. A link must be an absolute `http` or `https` address, is rendered
 * through `[href]` and never as HTML, opens with `rel="noopener noreferrer"`, and is never fetched
 * by the server. No file is uploaded anywhere.
 */
@Component({
  selector: 'app-study',
  imports: [ReactiveFormsModule],
  templateUrl: './study.component.html',
  styleUrl: './study.component.scss',
})
export class StudyComponent {
  private readonly studyService = inject(StudyService);
  private readonly todayService = inject(TodayService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly statuses = STUDY_PROJECT_STATUSES;
  protected readonly resourceTypes = STUDY_RESOURCE_TYPES;

  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly saveSuccess = signal<string | null>(null);
  protected readonly pendingSessionId = signal<string | null>(null);

  protected readonly projects = signal<readonly StudyProject[]>([]);
  protected readonly sessions = signal<readonly StudySession[]>([]);
  protected readonly weekAnchor = signal<IsoLocalDate>(startOfWeek(toIsoLocalDate(new Date())));
  protected readonly todayDate = signal<IsoLocalDate>(toIsoLocalDate(new Date()));

  protected readonly isProjectFormOpen = signal(false);
  protected readonly editingProjectId = signal<string | null>(null);
  protected readonly isSessionFormOpen = signal(false);
  protected readonly editingSessionId = signal<string | null>(null);

  protected readonly projectForm = this.formBuilder.group({
    name: this.formBuilder.control('', [trimmedLength(1, 150)]),
    description: this.formBuilder.control('', [optionalTrimmedLength(2000)]),
    status: this.formBuilder.control<StudyProjectStatus>('active'),
    resources: this.formBuilder.array<ReturnType<StudyComponent['createResourceGroup']>>([]),
  });

  protected readonly sessionForm = this.formBuilder.group({
    studyProjectId: this.formBuilder.control(''),
    localDate: this.formBuilder.control(''),
    startTime: this.formBuilder.control(''),
    durationMinutes: this.formBuilder.control('', [requiredInteger(1, 1440)]),
    summary: this.formBuilder.control('', [optionalTrimmedLength(1000)]),
    progressNote: this.formBuilder.control('', [optionalTrimmedLength(1000)]),
  });

  protected readonly weekLabel = computed(() => {
    const start = this.weekAnchor();
    const end = addDays(start, 6);

    return `${formatShortDate(start)} to ${formatShortDate(end)}`;
  });

  /** The week laid out Monday to Sunday, as the mockup describes. */
  protected readonly week = computed<readonly StudyDay[]>(() =>
    weekDays(this.weekAnchor()).map((date, index) => {
      const daySessions = this.sessions().filter((session) => session.localDate === date);

      return {
        date,
        label: formatShortDate(date),
        weekdayName: WEEKDAY_NAMES[index],
        sessions: daySessions,
        minutes: daySessions.reduce((total, session) => total + session.durationMinutes, 0),
      };
    }),
  );

  protected readonly weekMinutes = computed(() =>
    this.sessions().reduce((total, session) => total + session.durationMinutes, 0),
  );

  protected readonly selectedProject = signal<StudyProject | null>(null);

  constructor() {
    this.todayService
      .getSummary()
      .pipe(take(1))
      .subscribe({
        next: (summary) => {
          this.todayDate.set(summary.localDate);
          this.weekAnchor.set(startOfWeek(summary.localDate));
          this.load();
        },
        error: () => this.load(),
      });
  }

  protected get resourceControls() {
    return this.projectForm.controls.resources;
  }

  protected load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const from = this.weekAnchor();
    const to = addDays(from, 6);

    forkJoin({
      projects: this.studyService.getProjects(),
      sessions: this.studyService.getSessions(from, to),
    })
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (result) => {
          this.projects.set(result.projects);
          this.sessions.set(result.sessions);

          const selected = this.selectedProject();

          if (selected !== null) {
            this.selectedProject.set(
              result.projects.find((project) => project.id === selected.id) ?? null,
            );
          }
        },
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected goToPreviousWeek(): void {
    this.weekAnchor.update((anchor) => addDays(anchor, -7));
    this.load();
  }

  protected goToNextWeek(): void {
    this.weekAnchor.update((anchor) => addDays(anchor, 7));
    this.load();
  }

  protected goToThisWeek(): void {
    this.weekAnchor.set(startOfWeek(this.todayDate()));
    this.load();
  }

  protected selectProject(project: StudyProject): void {
    this.selectedProject.update((current) => (current?.id === project.id ? null : project));
  }

  protected minutesLabel(minutes: number): string {
    return formatMinutes(minutes);
  }

  protected isToday(date: IsoLocalDate): boolean {
    return date === this.todayDate();
  }

  protected openProjectForm(project?: StudyProject): void {
    this.editingProjectId.set(project?.id ?? null);
    this.formError.set(null);
    this.saveSuccess.set(null);
    this.resourceControls.clear();

    for (const resource of project?.resources ?? []) {
      this.resourceControls.push(
        this.createResourceGroup(
          resource.title,
          resource.resourceType,
          resource.externalUrl ?? '',
          resource.notes ?? '',
        ),
      );
    }

    this.projectForm.patchValue({
      name: project?.name ?? '',
      description: project?.description ?? '',
      status: project?.status ?? 'active',
    });
    this.isProjectFormOpen.set(true);
  }

  protected closeProjectForm(): void {
    this.isProjectFormOpen.set(false);
    this.formError.set(null);
  }

  protected addResource(): void {
    this.resourceControls.push(this.createResourceGroup());
  }

  protected removeResource(index: number): void {
    this.resourceControls.removeAt(index);
  }

  protected saveProject(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);

    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      this.formError.set(
        this.resourceControls.invalid
          ? 'Every resource needs a title, and any link must start with http:// or https://.'
          : 'Enter a name for the project.',
      );

      return;
    }

    const value = this.projectForm.getRawValue();
    const request = {
      name: trimValue(value.name),
      description: trimToNull(value.description),
      status: value.status,
      resources: value.resources.map((resource) => ({
        title: trimValue(resource.title),
        resourceType: resource.resourceType,
        externalUrl: trimToNull(resource.externalUrl),
        notes: trimToNull(resource.notes),
      })),
    };

    const editingId = this.editingProjectId();

    this.runSave(
      editingId === null
        ? this.studyService.createProject(request)
        : this.studyService.updateProject(editingId, request),
      editingId === null ? 'Project created.' : 'Project updated.',
      () => this.isProjectFormOpen.set(false),
    );
  }

  protected openSessionForm(session?: StudySession, date?: IsoLocalDate): void {
    this.editingSessionId.set(session?.id ?? null);
    this.formError.set(null);
    this.saveSuccess.set(null);
    this.sessionForm.reset({
      studyProjectId:
        session?.studyProjectId ?? this.selectedProject()?.id ?? this.projects()[0]?.id ?? '',
      localDate: session?.localDate ?? date ?? this.todayDate(),
      startTime: session?.startTime === null || session === undefined ? '' : session.startTime,
      durationMinutes: session === undefined ? '' : String(session.durationMinutes),
      summary: session?.summary ?? '',
      progressNote: session?.progressNote ?? '',
    });
    this.isSessionFormOpen.set(true);
  }

  protected closeSessionForm(): void {
    this.isSessionFormOpen.set(false);
    this.formError.set(null);
  }

  protected saveSession(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);

    const value = this.sessionForm.getRawValue();

    if (trimValue(value.studyProjectId).length === 0) {
      this.formError.set('Choose the project you studied.');

      return;
    }

    if (this.sessionForm.invalid) {
      this.sessionForm.markAllAsTouched();
      this.formError.set('Enter how many minutes you studied.');

      return;
    }

    const request = {
      studyProjectId: value.studyProjectId,
      localDate: value.localDate,
      startTime: trimToNull(value.startTime),
      durationMinutes: parseInteger(value.durationMinutes) ?? 0,
      summary: trimToNull(value.summary),
      progressNote: trimToNull(value.progressNote),
    };

    const editingId = this.editingSessionId();

    this.runSave(
      editingId === null
        ? this.studyService.createSession(request)
        : this.studyService.updateSession(editingId, request),
      editingId === null ? 'Study session recorded.' : 'Study session updated.',
      () => this.isSessionFormOpen.set(false),
    );
  }

  protected deleteSession(session: StudySession): void {
    if (this.pendingSessionId() !== null) {
      return;
    }

    if (!window.confirm('Delete this study session? This cannot be undone.')) {
      return;
    }

    this.pendingSessionId.set(session.id);

    this.studyService
      .deleteSession(session.id)
      .pipe(
        take(1),
        finalize(() => this.pendingSessionId.set(null)),
      )
      .subscribe({
        next: () => {
          this.saveSuccess.set('Study session deleted.');
          this.load();
        },
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  private createResourceGroup(
    title = '',
    resourceType: StudyResourceType = 'article',
    externalUrl = '',
    notes = '',
  ) {
    return this.formBuilder.group<ResourceGroupControls>({
      title: this.formBuilder.control(title, [trimmedLength(1, 200)]),
      resourceType: this.formBuilder.control<StudyResourceType>(resourceType),
      // The same rule the server applies, so the user is told immediately instead of after a
      // round trip. The server check is the one that actually enforces it.
      externalUrl: this.formBuilder.control(externalUrl, [safeExternalUrl]),
      notes: this.formBuilder.control(notes, [optionalTrimmedLength(1000)]),
    });
  }

  private runSave(
    request: Observable<unknown>,
    successMessage: string,
    onSuccess: () => void,
  ): void {
    this.isSaving.set(true);

    request
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          onSuccess();
          this.saveSuccess.set(successMessage);
          this.load();
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.formError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }
}
