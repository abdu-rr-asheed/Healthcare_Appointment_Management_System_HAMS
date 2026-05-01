import {
  AppointmentType,
  ClinicianStatus,
  ReportType,
  ReportFormat,
  TimeOfDay
} from './enums';
import {
  Address,
  RegularSchedule,
  LeavePeriod,
  SlotConfiguration
} from './entities';

export interface RegisterRequest {
  nhsNumber: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  dateOfBirth: string;
  firstName: string;
  lastName: string;
  address: Address;
  mfaEnabled: boolean;
}

export interface LoginRequest {
  nhsNumber: string;
  password: string;
}

export interface MfaVerificationRequest {
  userId: string;
  code: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface BookAppointmentRequest {
  slotId: string;
  appointmentType: AppointmentType;
  notes?: string;
}

export interface RescheduleRequest {
  newSlotId: string;
  reason?: string;
}

export interface CancelAppointmentRequest {
  reason: string;
  acknowledgeLateNotice: boolean;
}

export interface DnaRequest {
  reason: string;
  followUpRequired: boolean;
  followUpNotes?: string;
}

export interface CreateClinicalNoteRequest {
  content: string;
  isPrivate: boolean;
  consultationType?: string;
  findings?: string;
  recommendations?: string;
}

export interface UpdateClinicalNoteRequest {
  content: string;
}

export interface UpdatePatientRequest {
  email?: string;
  phoneNumber?: string;
  address?: Address;
}

export interface UpdateClinicianRequest {
  email?: string;
  phoneNumber?: string;
  specialty?: string;
  departmentId?: string;
  qualifications?: string[];
  status?: ClinicianStatus;
}

export interface UpdateAvailabilityRequest {
  regularSchedule: RegularSchedule[];
  leavePeriods: LeavePeriod[];
  slotConfigurations: SlotConfiguration[];
}

export interface GenerateSlotsRequest {
  startDate: string;
  endDate: string;
}

export interface GenerateReportRequest {
  reportType: ReportType;
  startDate: string;
  endDate: string;
  filters?: ReportFilters;
  format: ReportFormat;
  includeCharts: boolean;
}

export interface ReportFilters {
  departmentIds?: string[];
  clinicianIds?: string[];
  appointmentTypes?: string[];
  status?: string[];
}

export interface GetSlotSuggestionsRequest {
  departmentId: string;
  clinicianId?: string;
  preferences: SlotPreferences;
}

export interface SlotPreferences {
  preferredTimeOfDay?: TimeOfDay;
  preferredDays?: number[];
  startDate?: string;
  endDate?: string;
}

export interface SyncPatientDataRequest {
  nhsNumber: string;
}
