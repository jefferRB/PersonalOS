import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { IsoLocalDate } from '../time/local-date';
import { JournalEntry, SaveJournalEntryRequest } from './journal.models';

/**
 * The daily reflection of the authenticated account.
 *
 * The date is part of the path and the text is always in the request body, never in a query
 * string, so a reflection can never end up in a browser history entry or a server access log.
 * Responses carry `Cache-Control: no-store`, and nothing here writes to browser storage.
 */
@Injectable({ providedIn: 'root' })
export class JournalService {
  private readonly http = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryService);

  get(date: IsoLocalDate): Observable<JournalEntry> {
    return this.http.get<JournalEntry>(`/api/journal/${date}`);
  }

  save(date: IsoLocalDate, request: SaveJournalEntryRequest): Observable<JournalEntry> {
    return this.antiforgery.protect(() =>
      this.http.put<JournalEntry>(`/api/journal/${date}`, request),
    );
  }
}
