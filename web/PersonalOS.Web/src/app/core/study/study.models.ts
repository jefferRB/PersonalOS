import { IsoLocalDate } from '../time/local-date';

/** Where a study project stands, exactly as the API spells it. */
export type StudyProjectStatus = 'active' | 'paused' | 'completed';

/** What kind of material a resource points to, exactly as the API spells it. */
export type StudyResourceType = 'audio' | 'video' | 'pdf' | 'exam' | 'article' | 'other';

/** Options offered by the project status picker. */
export const STUDY_PROJECT_STATUSES: readonly {
  value: StudyProjectStatus;
  label: string;
}[] = [
  { value: 'active', label: 'Active' },
  { value: 'paused', label: 'Paused' },
  { value: 'completed', label: 'Completed' },
];

/** Options offered by the resource type picker. */
export const STUDY_RESOURCE_TYPES: readonly {
  value: StudyResourceType;
  label: string;
}[] = [
  { value: 'audio', label: 'Audio' },
  { value: 'video', label: 'Video' },
  { value: 'pdf', label: 'PDF' },
  { value: 'exam', label: 'Exam' },
  { value: 'article', label: 'Article' },
  { value: 'other', label: 'Other' },
];

/**
 * A reference to study material.
 *
 * `externalUrl` is always an `http` or `https` address: the server rejects any other scheme
 * before storing it. Templates still render it only through `[href]`, never as HTML.
 */
export interface StudyResource {
  readonly id: string;
  readonly title: string;
  readonly resourceType: StudyResourceType;
  readonly externalUrl: string | null;
  readonly notes: string | null;
}

/** A subject or learning project with its material. */
export interface StudyProject {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly status: StudyProjectStatus;
  readonly resources: readonly StudyResource[];
}

/** One recorded block of studying. */
export interface StudySession {
  readonly id: string;
  readonly studyProjectId: string;
  readonly projectName: string;
  readonly localDate: IsoLocalDate;
  readonly startTime: string | null;
  readonly durationMinutes: number;
  readonly summary: string | null;
  readonly progressNote: string | null;
}

/** Values sent for one study resource. */
export interface StudyResourceRequest {
  readonly title: string;
  readonly resourceType: StudyResourceType;
  readonly externalUrl: string | null;
  readonly notes: string | null;
}

/** Values sent when creating or editing a study project. */
export interface SaveStudyProjectRequest {
  readonly name: string;
  readonly description: string | null;
  readonly status: StudyProjectStatus;
  readonly resources: readonly StudyResourceRequest[];
}

/** Values sent when recording or editing a study session. */
export interface SaveStudySessionRequest {
  readonly studyProjectId: string;
  readonly localDate: IsoLocalDate;
  readonly startTime: string | null;
  readonly durationMinutes: number;
  readonly summary: string | null;
  readonly progressNote: string | null;
}
