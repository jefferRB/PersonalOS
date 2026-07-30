import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CurrentUser } from '../../../core/auth/auth.models';
import { AuthStore } from '../../../core/auth/auth.store';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  let fixture: ComponentFixture<NotFoundComponent>;
  let store: AuthStore;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    store = TestBed.inject(AuthStore);
  });

  it('links anonymous users back to login', () => {
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();

    expect(pageText()).toContain('Page not found');
    expect(query<HTMLAnchorElement>('a').getAttribute('href')).toBe('/login');
  });

  it('links authenticated users back to Today', () => {
    store.setAuthenticated(user);
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();

    expect(query<HTMLAnchorElement>('a').getAttribute('href')).toBe('/app/today');
  });

  function query<T extends HTMLElement>(selector: string): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;
    expect(element).not.toBeNull();

    return element as T;
  }

  function pageText(): string {
    return fixture.nativeElement.textContent ?? '';
  }
});
