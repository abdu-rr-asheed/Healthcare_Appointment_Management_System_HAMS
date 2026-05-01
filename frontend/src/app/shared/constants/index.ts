import { UserRole, AppointmentType, AppointmentStatus, ClinicianStatus, NotificationType, LeaveType, ReportType, ReportFormat, TimeOfDay } from '../models/enums';

export const USER_ROLES = {
  PATIENT: UserRole.Patient,
  CLINICIAN: UserRole.Clinician,
  ADMINISTRATOR: UserRole.Administrator
} as const;

export const APPOINTMENT_TYPES = {
  INITIAL_CONSULTATION: AppointmentType.InitialConsultation,
  FOLLOW_UP: AppointmentType.FollowUp,
  EMERGENCY: AppointmentType.Emergency
} as const;

export const APPOINTMENT_STATUSES = {
  PENDING: AppointmentStatus.Pending,
  CONFIRMED: AppointmentStatus.Confirmed,
  CANCELLED: AppointmentStatus.Cancelled,
  COMPLETED: AppointmentStatus.Completed,
  DID_NOT_ATTEND: AppointmentStatus.DidNotAttend
} as const;

export const CLINICIAN_STATUSES = {
  ACTIVE: ClinicianStatus.Active,
  INACTIVE: ClinicianStatus.Inactive,
  ON_LEAVE: ClinicianStatus.OnLeave
} as const;

export const NOTIFICATION_TYPES = {
  APPOINTMENT_REMINDER: NotificationType.AppointmentReminder,
  BOOKING_CONFIRMATION: NotificationType.BookingConfirmation,
  CANCELLATION: NotificationType.Cancellation,
  RESCHEDULE: NotificationType.Reschedule,
  SYSTEM: NotificationType.System
} as const;

export const LEAVE_TYPES = {
  ANNUAL: LeaveType.Annual,
  SICK: LeaveType.Sick,
  STUDY: LeaveType.Study,
  OTHER: LeaveType.Other
} as const;

export const REPORT_TYPES = {
  BOOKING_SUMMARY: ReportType.BookingSummary,
  CANCELLATION_ANALYSIS: ReportType.CancellationAnalysis,
  DNA_REPORT: ReportType.DnaReport,
  SLOT_UTILISATION: ReportType.SlotUtilisation,
  CUSTOM: ReportType.Custom
} as const;

export const REPORT_FORMATS = {
  CSV: ReportFormat.CSV,
  PDF: ReportFormat.PDF
} as const;

export const TIME_OF_DAY = {
  MORNING: TimeOfDay.Morning,
  AFTERNOON: TimeOfDay.Afternoon,
  EVENING: TimeOfDay.Evening,
  ANY: TimeOfDay.Any
} as const;

export const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' }
] as const;

export const SLOT_DURATION_OPTIONS = [
  { value: 15, label: '15 minutes' },
  { value: 30, label: '30 minutes' },
  { value: 45, label: '45 minutes' },
  { value: 60, label: '1 hour' },
  { value: 90, label: '1.5 hours' },
  { value: 120, label: '2 hours' }
] as const;

export const BUFFER_TIME_OPTIONS = [
  { value: 0, label: 'No buffer' },
  { value: 5, label: '5 minutes' },
  { value: 10, label: '10 minutes' },
  { value: 15, label: '15 minutes' },
  { value: 30, label: '30 minutes' }
] as const;

export const NOTICE_PERIOD_HOURS = 24;

export const REMINDER_INTERVALS = {
  FORTY_EIGHT_HOURS: 48,
  TWO_HOURS: 2
} as const;

export const SESSION_TIMEOUT_MINUTES = 60;

export const JWT_CLAIM_TYPES = {
  NAME_IDENTIFIER: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  ROLE: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  EMAIL: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
} as const;

export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'hams_access_token',
  REFRESH_TOKEN: 'hams_refresh_token',
  USER_DATA: 'hams_user_data',
  THEME: 'hams_theme',
  LANGUAGE: 'hams_language',
  LAST_LOGIN: 'hams_last_login'
} as const;

export const API_ENDPOINTS = {
  AUTH: {
    REGISTER: '/api/auth/register',
    LOGIN: '/api/auth/login',
    VERIFY_MFA: '/api/auth/verify-mfa',
    LOGOUT: '/api/auth/logout',
    REFRESH_TOKEN: '/api/auth/refresh-token',
    ME: '/api/auth/me'
  },
  APPOINTMENTS: {
    BASE: '/api/appointments',
    SLOTS: '/api/appointments/slots',
    UPCOMING: '/api/appointments/upcoming',
    HISTORY: '/api/appointments/history',
    SUGGESTIONS: '/api/appointments/suggestions'
  },
  PATIENTS: {
    BASE: '/api/patients',
    ME: '/api/patients/me',
    NOTIFICATIONS: '/api/patients/notifications'
  },
  CLINICIANS: {
    BASE: '/api/clinicians',
    ME: '/api/clinicians/me',
    AVAILABILITY: '/api/clinicians/me/availability',
    SCHEDULE: '/api/clinicians/me/schedule'
  },
  REPORTS: {
    BASE: '/api/admin/reports',
    GENERATE: '/api/admin/reports/generate',
    TYPES: '/api/admin/reports/types'
  },
  DEPARTMENTS: {
    BASE: '/api/departments'
  },
  EHR: {
    PATIENT_DATA: '/api/ehr/patient',
    MEDICAL_HISTORY: '/api/ehr/patient',
    ALLERGIES: '/api/ehr/patient',
    MEDICATIONS: '/api/ehr/patient',
    SYNC: '/api/ehr/sync',
    HEALTH: '/api/ehr/health'
  },
  AUDIT: {
    BASE: '/api/admin/audit-log'
  }
} as const;

export const HTTP_STATUS_CODES = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  UNPROCESSABLE_ENTITY: 422,
  TOO_MANY_REQUESTS: 429,
  INTERNAL_SERVER_ERROR: 500,
  SERVICE_UNAVAILABLE: 503
} as const;

export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 20,
  MAX_PAGE_SIZE: 100,
  MIN_PAGE_SIZE: 1
} as const;

export const DATE_FORMATS = {
  DISPLAY: 'DD MMM YYYY',
  DISPLAY_WITH_TIME: 'DD MMM YYYY HH:mm',
  ISO: 'YYYY-MM-DDTHH:mm:ss',
  DATE_PICKER: 'YYYY-MM-DD',
  TIME_PICKER: 'HH:mm'
} as const;

export const VALIDATION_MESSAGES = {
  REQUIRED: 'This field is required',
  EMAIL_INVALID: 'Please enter a valid email address',
  PHONE_INVALID: 'Please enter a valid phone number',
  NHS_NUMBER_INVALID: 'Please enter a valid NHS number (10 digits)',
  PASSWORD_TOO_SHORT: 'Password must be at least 8 characters',
  PASSWORD_NO_UPPERCASE: 'Password must contain at least one uppercase letter',
  PASSWORD_NO_LOWERCASE: 'Password must contain at least one lowercase letter',
  PASSWORD_NO_NUMBER: 'Password must contain at least one number',
  PASSWORD_NO_SPECIAL: 'Password must contain at least one special character',
  PASSWORDS_MISMATCH: 'Passwords do not match',
  DATE_IN_PAST: 'Date cannot be in the past',
  END_DATE_BEFORE_START: 'End date must be after start date',
  NOTICE_PERIOD_WARNING: 'Cancellations within 24 hours may incur penalties',
  CONFIRM_REQUIRED: 'Please confirm this action'
} as const;

export const SUCCESS_MESSAGES = {
  BOOKING_CONFIRMED: 'Appointment booked successfully! Confirmation sent to your email.',
  APPOINTMENT_RESCHEDULED: 'Appointment rescheduled successfully!',
  APPOINTMENT_CANCELLED: 'Appointment cancelled successfully!',
  CLINICAL_NOTE_SAVED: 'Clinical note saved successfully!',
  CLINICAL_NOTE_SYNCED: 'Clinical note synced to EHR successfully!',
  PROFILE_UPDATED: 'Profile updated successfully!',
  AVAILABILITY_UPDATED: 'Availability updated successfully!',
  SLOTS_GENERATED: 'Appointment slots generated successfully!',
  REPORT_GENERATED: 'Report generated successfully!',
  PASSWORD_CHANGED: 'Password changed successfully!'
} as const;

export const ERROR_MESSAGES = {
  GENERIC: 'An error occurred. Please try again.',
  NETWORK: 'Network error. Please check your connection.',
  UNAUTHORIZED: 'You are not authorized to perform this action.',
  FORBIDDEN: 'Access denied.',
  NOT_FOUND: 'Resource not found.',
  SLOT_UNAVAILABLE: 'This slot is no longer available.',
  INVALID_TOKEN: 'Your session has expired. Please log in again.',
  RATE_LIMITED: 'Too many requests. Please wait before trying again.'
} as const;

export const TOAST_CONFIG = {
  SUCCESS: {
    duration: 5000,
    type: 'success'
  },
  ERROR: {
    duration: 8000,
    type: 'error'
  },
  WARNING: {
    duration: 6000,
    type: 'warning'
  },
  INFO: {
    duration: 4000,
    type: 'info'
  }
} as const;

export const ROUTE_PATHS = {
  LOGIN: '/auth/login',
  REGISTER: '/auth/register',
  FORGOT_PASSWORD: '/auth/forgot-password',
  RESET_PASSWORD: '/auth/reset-password',
  MFA_VERIFY: '/auth/mfa-verification',
  PATIENT_DASHBOARD: '/patient/dashboard',
  PATIENT_BOOKING: '/patient/booking',
  PATIENT_HISTORY: '/patient/history',
  PATIENT_RESCHEDULE: '/patient/reschedule',
  PATIENT_CANCEL: '/patient/cancel',
  PATIENT_PROFILE: '/patient/profile',
  CLINICIAN_DASHBOARD: '/clinician/dashboard',
  CLINICIAN_SCHEDULE: '/clinician/schedule',
  CLINICIAN_AVAILABILITY: '/clinician/availability',
  CLINICIAN_CLINICAL_NOTES: '/clinician/clinical-notes',
  ADMIN_DASHBOARD: '/admin/dashboard',
  ADMIN_REPORTS: '/admin/reports',
  ADMIN_AUDIT_LOG: '/admin/audit-log',
  ADMIN_USER_MANAGEMENT: '/admin/users',
  ADMIN_CLINICIAN_PROFILES: '/admin/clinicians',
  UNAUTHORIZED: '/unauthorized',
  NOT_FOUND: '/404'
} as const;

export const ROLE_ROUTES = {
  [UserRole.Patient]: [
    ROUTE_PATHS.PATIENT_DASHBOARD,
    ROUTE_PATHS.PATIENT_BOOKING,
    ROUTE_PATHS.PATIENT_HISTORY,
    ROUTE_PATHS.PATIENT_RESCHEDULE,
    ROUTE_PATHS.PATIENT_CANCEL,
    ROUTE_PATHS.PATIENT_PROFILE
  ],
  [UserRole.Clinician]: [
    ROUTE_PATHS.CLINICIAN_DASHBOARD,
    ROUTE_PATHS.CLINICIAN_SCHEDULE,
    ROUTE_PATHS.CLINICIAN_AVAILABILITY,
    ROUTE_PATHS.CLINICIAN_CLINICAL_NOTES
  ],
  [UserRole.Administrator]: [
    ROUTE_PATHS.ADMIN_DASHBOARD,
    ROUTE_PATHS.ADMIN_REPORTS,
    ROUTE_PATHS.ADMIN_AUDIT_LOG,
    ROUTE_PATHS.ADMIN_USER_MANAGEMENT,
    ROUTE_PATHS.ADMIN_CLINICIAN_PROFILES
  ]
} as const;
