import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CurrentUser } from '../../core/auth/auth.models';
import { AuthStore } from '../../core/auth/auth.store';
import { TodayComponent } from './today.component';

describe('TodayComponent', () => {
  let fixture: ComponentFixture<TodayComponent>;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodayComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    TestBed.inject(AuthStore).setAuthenticated(user);
    fixture = TestBed.createComponent(TodayComponent);
    fixture.detectChanges();
  });

  it('renders the authenticated display name and truthful empty state', () => {
    const text = fixture.nativeElement.textContent ?? '';

    expect(text).toContain('Good day, Jefferson.');
    expect(text).toContain('Today is ready for real data.');
    expect(text).toContain('not implemented yet');
  });
});
