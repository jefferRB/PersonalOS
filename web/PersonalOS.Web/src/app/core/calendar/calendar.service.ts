import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { IsoLocalDate } from '../time/local-date';
import {
  CalendarDay,
  CalendarMonth,
  CalendarOccurrence,
  OccurrenceStatus,
  PlanningItem,
  SavePlanningItemRequest,
  UpcomingWeek,
} from './calendar.models';

/**
 * The calendar of the authenticated account.
 *
 * Reads are plain requests; every write goes through the antiforgery guard. Nothing is cached in a
 * field and nothing is written to browser storage: the server owns the data and each screen holds
 * the current answer in its own signals for as long as it is on screen.
 */
@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly http = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryService);

  /** Reads the summaries the month grid needs. */
  getMonth(year: number, month: number): Observable<CalendarMonth> {
    return this.http.get<CalendarMonth>('/api/calendar/month', {
      params: { year, month },
    });
  }

  /**
   * Reads everything on one local calendar day.
   *
   * Omitting the date lets the server decide which day is current from the saved time zone, which
   * is the only correct answer when the device is somewhere else.
   */
  getDay(date?: IsoLocalDate): Observable<CalendarDay> {
    return this.http.get<CalendarDay>('/api/calendar/day', {
      params: date === undefined ? {} : { date },
    });
  }

  /**
   * Reads the next seven local days.
   *
   * Everything in the window arrives, so the section's filters run on the client instead of costing
   * a request per click.
   */
  getUpcoming(from?: IsoLocalDate): Observable<UpcomingWeek> {
    return this.http.get<UpcomingWeek>('/api/calendar/upcoming', {
      params: from === undefined ? {} : { from },
    });
  }

  /** Reads one item with its rule, for the editor. */
  getItem(id: string): Observable<PlanningItem> {
    return this.http.get<PlanningItem>(`/api/calendar/items/${id}`);
  }

  create(request: SavePlanningItemRequest): Observable<PlanningItem> {
    return this.antiforgery.protect(() =>
      this.http.post<PlanningItem>('/api/calendar/items', request),
    );
  }

  update(id: string, request: SavePlanningItemRequest): Observable<PlanningItem> {
    return this.antiforgery.protect(() =>
      this.http.put<PlanningItem>(`/api/calendar/items/${id}`, request),
    );
  }

  /** Deletes an item, and with it the whole series. */
  delete(id: string): Observable<void> {
    return this.antiforgery.protect(() =>
      this.http.delete<void>(`/api/calendar/items/${id}`),
    );
  }

  /**
   * Records what the user decided about one occurrence.
   *
   * The call is idempotent on the server, so a double click costs a request and changes nothing.
   */
  setOccurrenceStatus(
    id: string,
    occurrenceDate: IsoLocalDate,
    status: OccurrenceStatus,
  ): Observable<CalendarOccurrence> {
    return this.antiforgery.protect(() =>
      this.http.put<CalendarOccurrence>(
        `/api/calendar/items/${id}/occurrences/${occurrenceDate}/status`,
        { status },
      ),
    );
  }
}
