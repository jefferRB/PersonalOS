import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { IsoLocalDate } from '../time/local-date';
import {
  RoutineOccurrence,
  RoutineSession,
  RoutineTemplate,
  SaveRoutineRequest,
  SaveRoutineSessionRequest,
} from './routines.models';

/**
 * Routines of the authenticated account and the sessions that execute them.
 */
@Injectable({ providedIn: 'root' })
export class RoutinesService {
  private readonly http = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryService);

  getTemplates(activeOnly = false): Observable<RoutineTemplate[]> {
    return this.http.get<RoutineTemplate[]>('/api/routines', {
      params: { activeOnly },
    });
  }

  getTemplate(id: string): Observable<RoutineTemplate> {
    return this.http.get<RoutineTemplate>(`/api/routines/${id}`);
  }

  /**
   * Reads which routines apply inside an inclusive local-date range.
   *
   * The server calculates these from the stored rules, so asking for a future month costs one
   * query and stores nothing.
   */
  getOccurrences(from: IsoLocalDate, to: IsoLocalDate): Observable<RoutineOccurrence[]> {
    return this.http.get<RoutineOccurrence[]>('/api/routines/occurrences', {
      params: { from, to },
    });
  }

  create(request: SaveRoutineRequest): Observable<RoutineTemplate> {
    return this.antiforgery.protect(() =>
      this.http.post<RoutineTemplate>('/api/routines', request),
    );
  }

  update(id: string, request: SaveRoutineRequest): Observable<RoutineTemplate> {
    return this.antiforgery.protect(() =>
      this.http.put<RoutineTemplate>(`/api/routines/${id}`, request),
    );
  }

  delete(id: string): Observable<void> {
    return this.antiforgery.protect(() => this.http.delete<void>(`/api/routines/${id}`));
  }

  /** Starts, or returns, the session of a routine on one local calendar day. */
  startSession(routineId: string, localDate: IsoLocalDate): Observable<RoutineSession> {
    return this.antiforgery.protect(() =>
      this.http.post<RoutineSession>(`/api/routines/${routineId}/sessions`, { localDate }),
    );
  }

  getSession(sessionId: string): Observable<RoutineSession> {
    return this.http.get<RoutineSession>(`/api/routine-sessions/${sessionId}`);
  }

  saveSession(
    sessionId: string,
    request: SaveRoutineSessionRequest,
  ): Observable<RoutineSession> {
    return this.antiforgery.protect(() =>
      this.http.put<RoutineSession>(`/api/routine-sessions/${sessionId}`, request),
    );
  }
}
