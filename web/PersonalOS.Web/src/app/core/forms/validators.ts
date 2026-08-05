import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validates a text control by its trimmed length.
 *
 * `Validators.required` accepts a value made only of spaces, and `Validators.minLength` counts the
 * untrimmed value. Both would let the client accept input the server rejects, so the rule is
 * expressed once here and reused by every feature that captures text.
 *
 * @param min Minimum length after trimming.
 * @param max Maximum length after trimming.
 */
export function trimmedLength(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = typeof control.value === 'string' ? control.value.trim() : '';

    if (value.length === 0) {
      return { required: true };
    }

    if (value.length < min) {
      return { minlength: { requiredLength: min, actualLength: value.length } };
    }

    if (value.length > max) {
      return { maxlength: { requiredLength: max, actualLength: value.length } };
    }

    return null;
  };
}

/**
 * Validates an optional text control by its trimmed length.
 *
 * An empty value is acceptable; only an over-long one is rejected.
 *
 * @param max Maximum length after trimming.
 */
export function optionalTrimmedLength(max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = typeof control.value === 'string' ? control.value.trim() : '';

    return value.length > max
      ? { maxlength: { requiredLength: max, actualLength: value.length } }
      : null;
  };
}

/**
 * Validates an optional external link the same way the server does.
 *
 * Only absolute `http` and `https` addresses are accepted. Rejecting `javascript:`, `data:`, and
 * every other scheme in the browser gives immediate feedback; the server repeats the check, which
 * is what actually enforces it.
 */
export const safeExternalUrl: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value = typeof control.value === 'string' ? control.value.trim() : '';

  if (value.length === 0) {
    return null;
  }

  let parsed: URL;

  try {
    parsed = new URL(value);
  } catch {
    return { externalUrl: true };
  }

  return parsed.protocol === 'http:' || parsed.protocol === 'https:'
    ? null
    : { externalUrl: true };
};

/**
 * Trims a form value, turning an empty result into `null`.
 *
 * Optional text fields are stored as `null` rather than as an empty string, so "never written"
 * stays distinguishable from "written and erased".
 */
export function trimToNull(value: string | null | undefined): string | null {
  const trimmed = typeof value === 'string' ? value.trim() : '';

  return trimmed.length === 0 ? null : trimmed;
}

/** Trims a form value for a required field. */
export function trimValue(value: string | null | undefined): string {
  return typeof value === 'string' ? value.trim() : '';
}

/**
 * Reads a control value as text, whatever value accessor produced it.
 *
 * Numeric fields are declared as string controls so the form can tell "nothing entered" apart
 * from "not a number". Angular still binds `<input type="number">` through its number value
 * accessor, which writes a real `number` into the control, so the raw value is normalized here
 * rather than assumed to be a string. Getting this wrong silently drops every number the user
 * typed, which is exactly the kind of failure that only a behavioural test catches.
 */
function readAsText(value: unknown): string {
  if (typeof value === 'number') {
    return Number.isFinite(value) ? String(value) : '';
  }

  return typeof value === 'string' ? value.trim() : '';
}

/**
 * Reads a numeric control as a whole number.
 *
 * @returns The parsed value, or `null` when the box is empty or does not hold a whole number.
 */
export function parseInteger(value: unknown): number | null {
  const text = readAsText(value);

  if (text.length === 0 || !/^-?\d+$/.test(text)) {
    return null;
  }

  const parsed = Number(text);

  return Number.isSafeInteger(parsed) ? parsed : null;
}

/**
 * Reads a numeric control as a decimal.
 *
 * @returns The parsed value, or `null` when the box is empty or does not hold a number.
 */
export function parseDecimal(value: unknown): number | null {
  const text = readAsText(value);

  if (text.length === 0 || !/^-?\d+([.,]\d+)?$/.test(text)) {
    return null;
  }

  const parsed = Number(text.replace(',', '.'));

  return Number.isFinite(parsed) ? parsed : null;
}

/**
 * Validates a required whole number inside a range.
 *
 * @param min Smallest accepted value.
 * @param max Largest accepted value.
 */
export function requiredInteger(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = readAsText(control.value);

    if (raw.length === 0) {
      return { required: true };
    }

    const parsed = parseInteger(raw);

    if (parsed === null) {
      return { integer: true };
    }

    return parsed < min || parsed > max ? { range: { min, max } } : null;
  };
}

/**
 * Validates an optional number inside a range.
 *
 * @param min Smallest accepted value.
 * @param max Largest accepted value.
 */
export function optionalNumber(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = readAsText(control.value);

    if (raw.length === 0) {
      return null;
    }

    const parsed = parseDecimal(raw);

    if (parsed === null) {
      return { number: true };
    }

    return parsed < min || parsed > max ? { range: { min, max } } : null;
  };
}

/**
 * Validates an optional whole number inside a range.
 *
 * @param min Smallest accepted value.
 * @param max Largest accepted value.
 */
export function optionalInteger(min: number, max: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = readAsText(control.value);

    if (raw.length === 0) {
      return null;
    }

    const parsed = parseInteger(raw);

    if (parsed === null) {
      return { integer: true };
    }

    return parsed < min || parsed > max ? { range: { min, max } } : null;
  };
}

/**
 * Rejects an end time that is not after the start time.
 *
 * The rule lives on the group because it needs both controls. It mirrors the server, which also
 * refuses an end without a start.
 *
 * @param startControlName Name of the start-time control.
 * @param endControlName Name of the end-time control.
 */
export function timeRange(startControlName: string, endControlName: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const start = trimValue(group.get(startControlName)?.value as string | null);
    const end = trimValue(group.get(endControlName)?.value as string | null);

    if (end.length === 0) {
      return null;
    }

    if (start.length === 0) {
      return { endWithoutStart: true };
    }

    return end > start ? null : { endBeforeStart: true };
  };
}
