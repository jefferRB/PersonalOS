import { IsoLocalDate } from '../time/local-date';

/**
 * The reflection written for one local calendar day.
 *
 * This is the most sensitive data the Angular application handles. It is held in component
 * signals while the journal screen is open and nowhere else: never in `localStorage`, never in
 * `sessionStorage`, never in IndexedDB, never in a query string, and never in an analytics event.
 */
export interface JournalEntry {
  readonly localDate: IsoLocalDate;
  readonly wentWell: string | null;
  readonly wentPoorly: string | null;
  readonly cause: string | null;
  readonly lesson: string | null;
  readonly adjustmentForTomorrow: string | null;
  readonly freeNotes: string | null;
  readonly updatedAtUtc: string | null;
  readonly hasContent: boolean;
}

/**
 * Values sent when saving a journal entry.
 *
 * The day travels in the route, never in the body, and the account comes from the authentication
 * cookie. Neither can be chosen by the client.
 */
export interface SaveJournalEntryRequest {
  readonly wentWell: string | null;
  readonly wentPoorly: string | null;
  readonly cause: string | null;
  readonly lesson: string | null;
  readonly adjustmentForTomorrow: string | null;
  readonly freeNotes: string | null;
}
