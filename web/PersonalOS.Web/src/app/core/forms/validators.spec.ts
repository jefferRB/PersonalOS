import { FormControl, FormGroup } from '@angular/forms';

import {
  optionalInteger,
  parseDecimal,
  parseInteger,
  requiredInteger,
  safeExternalUrl,
  timeRange,
  trimToNull,
  trimmedLength,
} from './validators';

describe('form validators', () => {
  describe('trimmedLength', () => {
    it('rejects a value made only of whitespace', () => {
      // Validators.required would accept this, and the server would then reject the save.
      const control = new FormControl('   ', trimmedLength(1, 10));

      expect(control.hasError('required')).toBe(true);
    });

    it('measures length after trimming', () => {
      expect(new FormControl('  ab  ', trimmedLength(2, 3)).valid).toBe(true);
      expect(new FormControl('abcd', trimmedLength(1, 3)).hasError('maxlength')).toBe(true);
    });
  });

  describe('numeric parsing', () => {
    it('reads a value typed as text', () => {
      expect(parseInteger('42')).toBe(42);
      expect(parseDecimal('62.5')).toBe(62.5);
    });

    it('reads a value the number value accessor already converted', () => {
      // `<input type="number">` writes a real number into the control. Assuming a string here
      // would silently discard every number the user typed.
      expect(parseInteger(42)).toBe(42);
      expect(parseDecimal(62.5)).toBe(62.5);
    });

    it('accepts a decimal comma', () => {
      expect(parseDecimal('62,5')).toBe(62.5);
    });

    it('returns null for anything that is not a number', () => {
      expect(parseInteger('')).toBeNull();
      expect(parseInteger('abc')).toBeNull();
      expect(parseInteger('4.5')).toBeNull();
      expect(parseDecimal(null)).toBeNull();
    });
  });

  describe('requiredInteger', () => {
    it('reports an empty box, a non-number, and an out-of-range value differently', () => {
      expect(new FormControl('', requiredInteger(1, 10)).hasError('required')).toBe(true);
      expect(new FormControl('abc', requiredInteger(1, 10)).hasError('integer')).toBe(true);
      expect(new FormControl('11', requiredInteger(1, 10)).hasError('range')).toBe(true);
      expect(new FormControl('5', requiredInteger(1, 10)).valid).toBe(true);
    });
  });

  describe('optionalInteger', () => {
    it('accepts an empty box but rejects an out-of-range value', () => {
      expect(new FormControl('', optionalInteger(1, 10)).valid).toBe(true);
      expect(new FormControl('0', optionalInteger(1, 10)).hasError('range')).toBe(true);
    });
  });

  describe('safeExternalUrl', () => {
    it.each([
      'javascript:alert(1)',
      'data:text/html,<script>alert(1)</script>',
      'vbscript:msgbox(1)',
      'file:///etc/passwd',
      'angular.dev',
    ])('rejects %s', (candidate) => {
      expect(new FormControl(candidate, safeExternalUrl).hasError('externalUrl')).toBe(true);
    });

    it.each(['http://example.com', 'https://angular.dev/guide/signals'])(
      'accepts %s',
      (candidate) => {
        expect(new FormControl(candidate, safeExternalUrl).valid).toBe(true);
      },
    );

    it('accepts an empty value, because the link is optional', () => {
      expect(new FormControl('', safeExternalUrl).valid).toBe(true);
    });
  });

  describe('timeRange', () => {
    function group(start: string, end: string): FormGroup {
      return new FormGroup(
        { startTime: new FormControl(start), endTime: new FormControl(end) },
        { validators: [timeRange('startTime', 'endTime')] },
      );
    }

    it('accepts an end after the start', () => {
      expect(group('09:00', '10:00').valid).toBe(true);
    });

    it('rejects an end that is not after the start', () => {
      expect(group('10:00', '09:00').hasError('endBeforeStart')).toBe(true);
      expect(group('10:00', '10:00').hasError('endBeforeStart')).toBe(true);
    });

    it('rejects an end with no start', () => {
      expect(group('', '10:00').hasError('endWithoutStart')).toBe(true);
    });

    it('accepts an item with no times at all', () => {
      expect(group('', '').valid).toBe(true);
    });
  });

  describe('trimToNull', () => {
    it('turns an empty or whitespace-only value into null', () => {
      expect(trimToNull('   ')).toBeNull();
      expect(trimToNull('')).toBeNull();
      expect(trimToNull(null)).toBeNull();
      expect(trimToNull('  kept  ')).toBe('kept');
    });
  });
});
