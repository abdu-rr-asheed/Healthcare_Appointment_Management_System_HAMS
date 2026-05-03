import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { VALIDATION_MESSAGES, NOTICE_PERIOD_HOURS } from '../constants';

export function requiredValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return { required: true };
    }
    return null;
  };
}

export function emailValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(value) ? null : { email: true };
  };
}

export function phoneNumberValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    const phoneRegex = /^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$/;
    return phoneRegex.test(value) ? null : { phone: true };
  };
}

export function nhsNumberValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw: string = (control.value ?? '').toString();
    if (!raw) return null;

    // Strip any spaces or dashes the user may have typed (e.g. "943 476 5919")
    const digits = raw.replace(/[\s\-]/g, '');

    // Must be exactly 10 numeric digits
    if (!/^\d{10}$/.test(digits)) {
      return { nhsNumber: true };
    }

    // NHS checksum: multiply each of the first 9 digits by a descending weight
    // (10, 9, 8 … 2), sum the products, compute remainder mod 11, then derive
    // the expected check digit as  11 − remainder.
    //   • remainder === 0  →  check digit = 11, but NHS uses 0 in that case
    //   • result    === 10 →  no valid 10th digit exists; the number is invalid
    //   • otherwise compare result with the actual 10th digit
    const weights = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    const sum = weights.reduce(
      (acc, w, i) => acc + w * parseInt(digits[i], 10), 0
    );
    const remainder  = sum % 11;
    const checkDigit = remainder === 0 ? 0 : 11 - remainder;

    if (checkDigit === 10) {
      return { nhsNumber: true };   // mathematically impossible digit — reject
    }

    return checkDigit === parseInt(digits[9], 10) ? null : { nhsNumber: true };
  };
}

export function passwordValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    const errors: ValidationErrors = {};
    
    if (value.length < 8) {
      errors['minLength'] = true;
    }
    if (!/[A-Z]/.test(value)) {
      errors['noUppercase'] = true;
    }
    if (!/[a-z]/.test(value)) {
      errors['noLowercase'] = true;
    }
    if (!/[0-9]/.test(value)) {
      errors['noNumber'] = true;
    }
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(value)) {
      errors['noSpecialChar'] = true;
    }
    
    return Object.keys(errors).length > 0 ? errors : null;
  };
}

export function passwordMatchValidator(controlName: string, matchingControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const passwordControl = control.get(controlName);
    const confirmPasswordControl = control.get(matchingControlName);
    
    if (!passwordControl || !confirmPasswordControl) {
      return null;
    }
    
    const password = passwordControl.value;
    const confirmPassword = confirmPasswordControl.value;
    
    if (password !== confirmPassword) {
      confirmPasswordControl.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    
    confirmPasswordControl.setErrors(null);
    return null;
  };
}

export function dateOfBirthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const dateOfBirth = new Date(value);
    const now = new Date();
    const maxDate = new Date(now.getFullYear() - 120, now.getMonth(), now.getDate());
    const minDate = new Date(now.getFullYear() - 16, now.getMonth(), now.getDate());
    
    if (dateOfBirth > now) {
      return { futureDate: true };
    }
    if (dateOfBirth < maxDate) {
      return { tooOld: true };
    }
    if (dateOfBirth > minDate) {
      return { tooYoung: true };
    }
    
    return null;
  };
}

export function dateRangeValidator(startDateControlName: string, endDateControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const startDateControl = control.get(startDateControlName);
    const endDateControl = control.get(endDateControlName);
    
    if (!startDateControl || !endDateControl) {
      return null;
    }
    
    const startDate = startDateControl.value ? new Date(startDateControl.value) : null;
    const endDate = endDateControl.value ? new Date(endDateControl.value) : null;
    
    if (!startDate || !endDate) {
      return null;
    }
    
    if (endDate <= startDate) {
      endDateControl.setErrors({ invalidRange: true });
      return { invalidRange: true };
    }
    
    endDateControl.setErrors(null);
    return null;
  };
}

export function futureDateValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const date = new Date(value);
    const now = new Date();
    
    if (date <= now) {
      return { pastDate: true };
    }
    
    return null;
  };
}

export function minDateValidator(minDate: Date): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const date = new Date(value);
    
    if (date < minDate) {
      return { minDate: true };
    }
    
    return null;
  };
}

export function noticePeriodValidator(bookingDateControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const bookingDateControl = control.get(bookingDateControlName);
    
    if (!bookingDateControl) {
      return null;
    }
    
    const bookingDate = bookingDateControl.value ? new Date(bookingDateControl.value) : null;
    
    if (!bookingDate) {
      return null;
    }
    
    const now = new Date();
    const noticePeriod = new Date(now.getTime() + NOTICE_PERIOD_HOURS * 60 * 60 * 1000);
    
    if (bookingDate < noticePeriod) {
      return { withinNoticePeriod: true };
    }
    
    return null;
  };
}

export function slotAvailabilityValidator(minimumHoursBeforeAppointment: number = 24): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const slotDate = new Date(value);
    const now = new Date();
    const minimumDate = new Date(now.getTime() + minimumHoursBeforeAppointment * 60 * 60 * 1000);
    
    if (slotDate < minimumDate) {
      return { slotUnavailable: true };
    }
    
    return null;
  };
}

export function minLengthValidator(minLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    if (value.length < minLength) {
      return { minLength: { requiredLength: minLength, actualLength: value.length } };
    }
    
    return null;
  };
}

export function maxLengthValidator(maxLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    if (value.length > maxLength) {
      return { maxLength: { requiredLength: maxLength, actualLength: value.length } };
    }
    
    return null;
  };
}

export function patternValidator(pattern: RegExp, errorKey: string = 'pattern'): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    if (!pattern.test(value)) {
      return { [errorKey]: { pattern } };
    }
    
    return null;
  };
}

export function noWhitespaceValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    if (value.trim().length === 0) {
      return { whitespace: true };
    }
    
    return null;
  };
}

export function timeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const timeRegex = /^([01]?[0-9]|2[0-3]):[0-5][0-9]$/;
    
    if (!timeRegex.test(value)) {
      return { invalidTime: true };
    }
    
    return null;
  };
}

export function endTimeValidator(startTimeControlName: string, endTimeControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const startTimeControl = control.get(startTimeControlName);
    const endTimeControl = control.get(endTimeControlName);
    
    if (!startTimeControl || !endTimeControl) {
      return null;
    }
    
    const startTime = startTimeControl.value;
    const endTime = endTimeControl.value;
    
    if (!startTime || !endTime) {
      return null;
    }
    
    const startMinutes = convertTimeToMinutes(startTime);
    const endMinutes = convertTimeToMinutes(endTime);
    
    if (endMinutes <= startMinutes) {
      endTimeControl.setErrors({ invalidEndTime: true });
      return { invalidEndTime: true };
    }
    
    endTimeControl.setErrors(null);
    return null;
  };
}

export function postCodeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const ukPostCodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i;
    
    if (!ukPostCodeRegex.test(value)) {
      return { invalidPostCode: true };
    }
    
    return null;
  };
}

export function mfaCodeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const mfaCodeRegex = /^\d{6}$/;
    
    if (!mfaCodeRegex.test(value)) {
      return { invalidMfaCode: true };
    }
    
    return null;
  };
}

export function confirmationReferenceValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    const refRegex = /^HAMS-\d{4}-[A-Z0-9]+$/i;
    
    if (!refRegex.test(value)) {
      return { invalidReference: true };
    }
    
    return null;
  };
}

export function notesMaxLengthValidator(maxLength: number = 500): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) {
      return null;
    }
    
    if (value.length > maxLength) {
      return { notesTooLong: { requiredLength: maxLength, actualLength: value.length } };
    }
    
    return null;
  };
}

export function fileValidator(allowedTypes: string[], maxSizeInMB: number = 5): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const file = control.value;
    
    if (!file) {
      return null;
    }
    
    const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
    
    if (file.size > maxSizeInBytes) {
      return { fileSizeExceeded: { maxSize: maxSizeInMB } };
    }
    
    if (!allowedTypes.includes(file.type)) {
      return { invalidFileType: { allowedTypes } };
    }
    
    return null;
  };
}

export function getValidatorErrorMessage(validatorName: string, validatorValue?: any): string {
  switch (validatorName) {
    case 'required':
      return VALIDATION_MESSAGES.REQUIRED;
    case 'email':
      return VALIDATION_MESSAGES.EMAIL_INVALID;
    case 'phone':
      return VALIDATION_MESSAGES.PHONE_INVALID;
    case 'nhsNumber':
      return VALIDATION_MESSAGES.NHS_NUMBER_INVALID;
    case 'minLength':
      return `Minimum length is ${validatorValue?.requiredLength || 0} characters`;
    case 'maxLength':
      return `Maximum length is ${validatorValue?.requiredLength || 0} characters`;
    case 'passwordMismatch':
      return VALIDATION_MESSAGES.PASSWORDS_MISMATCH;
    case 'futureDate':
    case 'pastDate':
      return VALIDATION_MESSAGES.DATE_IN_PAST;
    case 'invalidRange':
      return VALIDATION_MESSAGES.END_DATE_BEFORE_START;
    case 'withinNoticePeriod':
      return VALIDATION_MESSAGES.NOTICE_PERIOD_WARNING;
    case 'whitespace':
      return VALIDATION_MESSAGES.REQUIRED;
    case 'invalidTime':
      return 'Please enter a valid time (HH:MM)';
    case 'invalidPostCode':
      return 'Please enter a valid UK postcode';
    case 'invalidMfaCode':
      return 'Please enter a valid 6-digit verification code';
    case 'noUppercase':
      return VALIDATION_MESSAGES.PASSWORD_NO_UPPERCASE;
    case 'noLowercase':
      return VALIDATION_MESSAGES.PASSWORD_NO_LOWERCASE;
    case 'noNumber':
      return VALIDATION_MESSAGES.PASSWORD_NO_NUMBER;
    case 'noSpecialChar':
      return VALIDATION_MESSAGES.PASSWORD_NO_SPECIAL;
    case 'tooYoung':
      return 'You must be at least 16 years old';
    case 'tooOld':
      return 'Invalid date of birth';
    case 'slotUnavailable':
      return 'This slot is no longer available for booking';
    case 'invalidEndTime':
      return 'End time must be after start time';
    case 'invalidReference':
      return 'Please enter a valid confirmation reference';
    case 'notesTooLong':
      return `Notes cannot exceed ${validatorValue?.requiredLength || 500} characters`;
    case 'fileSizeExceeded':
      return `File size cannot exceed ${validatorValue?.maxSize || 5} MB`;
    case 'invalidFileType':
      return 'Invalid file type';
    default:
      return VALIDATION_MESSAGES.REQUIRED;
  }
}

function convertTimeToMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
}
