export function isValidEmail(email: string): boolean {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
}

export function isValidPhone(phone: string): boolean {
  const phoneRegex = /^\+?[\d\s\-()]{10,20}$/;
  return phoneRegex.test(phone);
}

export function isValidNhsNumber(nhsNumber: string): boolean {
  if (!nhsNumber || nhsNumber.length !== 10 || !/^\d{10}$/.test(nhsNumber)) {
    return false;
  }
  
  const digits = nhsNumber.split('').map(Number);
  const checkDigit = digits[9];
  
  let sum = 0;
  const multipliers = [10, 9, 8, 7, 6, 5, 4, 3, 2];
  
  for (let i = 0; i < 9; i++) {
    sum += digits[i] * multipliers[i];
  }
  
  const remainder = sum % 11;
  const calculatedCheck = 11 - remainder;
  
  if (calculatedCheck === 11) return checkDigit === 0;
  if (calculatedCheck === 10) return false;
  return checkDigit === calculatedCheck;
}

export function isValidPostcode(postcode: string): boolean {
  const postcodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i;
  return postcodeRegex.test(postcode);
}

export function isValidDateOfBirth(dateOfBirth: string | Date): boolean {
  const dob = new Date(dateOfBirth);
  const today = new Date();
  
  if (isNaN(dob.getTime())) return false;
  if (dob > today) return false;
  
  const age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    return age - 1 >= 0;
  }
  
  return age >= 0 && age <= 150;
}

export function sanitizeInput(input: string): string {
  return input.replace(/<[^>]*>/g, '').trim();
}

export function sanitizeHtml(html: string): string {
  const div = document.createElement('div');
  div.textContent = html;
  return div.innerHTML;
}