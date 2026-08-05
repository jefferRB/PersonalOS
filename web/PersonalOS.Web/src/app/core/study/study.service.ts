import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { IsoLocalDate } from '../time/local-date';
import {
  SaveStudyProjectRequest,
  SaveStudySessionRequest,
  StudyProject,
  StudySession,
} from './study.models';

/**
 * Study projects of the authenticated account and the sessions recorded against them.
 */
@Injectable({ providedIn: 'root' })
export class StudyService {
  private readonly http = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryService);

  getProjects(): Observable<StudyProject[]> {
    return this.http.get<StudyProject[]>('/api/study/projects');
  }

  createProject(request: SaveStudyProjectRequest): Observable<StudyProject> {
    return this.antiforgery.protect(() =>
      this.http.post<StudyProject>('/api/study/projects', request),
    );
  }

  updateProject(id: string, request: SaveStudyProjectRequest): Observable<StudyProject> {
    return this.antiforgery.protect(() =>
      this.http.put<StudyProject>(`/api/study/projects/${id}`, request),
    );
  }

  getSessions(from: IsoLocalDate, to: IsoLocalDate): Observable<StudySession[]> {
    return this.http.get<StudySession[]>('/api/study/sessions', { params: { from, to } });
  }

  createSession(request: SaveStudySessionRequest): Observable<StudySession> {
    return this.antiforgery.protect(() =>
      this.http.post<StudySession>('/api/study/sessions', request),
    );
  }

  updateSession(id: string, request: SaveStudySessionRequest): Observable<StudySession> {
    return this.antiforgery.protect(() =>
      this.http.put<StudySession>(`/api/study/sessions/${id}`, request),
    );
  }

  deleteSession(id: string): Observable<void> {
    return this.antiforgery.protect(() =>
      this.http.delete<void>(`/api/study/sessions/${id}`),
    );
  }
}
