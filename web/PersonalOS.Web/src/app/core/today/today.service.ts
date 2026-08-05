import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { IsoLocalDate } from '../time/local-date';
import { TodaySummary } from './today.models';

/**
 * The integrated view of one local day.
 *
 * One request returns planning, routines, nutrition, and study together, so Today never renders
 * half a day while the rest is still arriving.
 */
@Injectable({ providedIn: 'root' })
export class TodayService {
  private readonly http = inject(HttpClient);

  /**
   * Reads the Today view.
   *
   * @param date Local calendar day to show. When omitted, the server decides which day it is from
   * the account's saved time zone rather than from the browser clock.
   */
  getSummary(date?: IsoLocalDate): Observable<TodaySummary> {
    return this.http.get<TodaySummary>('/api/today', {
      params: date === undefined ? {} : { date },
    });
  }
}
