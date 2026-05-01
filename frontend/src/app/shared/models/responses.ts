export interface AuthResponse {
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: UserDto;
  requiresMfa?: boolean;
  userId?: string;
  message?: string;
}

export interface UserDto {
  id: string;
  email: string;
  role: UserRole;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  requiresMfa: boolean;
}

export interface PatientProfileDto {
  id: string;
  userId: string;
  nhsNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phoneNumber: string;
  address: Address;
  createdAt: string;
}

export interface ClinicianProfileDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  specialty: string;
  departmentId: string;
  departmentName: string;
  qualifications: string[];
  licenseNumber: string;
  status: ClinicianStatus;
  createdAt: string;
}

export interface AppointmentDto {
  id: string;
  confirmationReference: string;
  patientId: string;
  slotId: string;
  startDateTime: string;
  endDateTime: string;
  clinicianId: string;
  clinicianName: string;
  departmentId: string;
  departmentName: string;
  type: AppointmentType;
  status: AppointmentStatus;
  notes?: string;
  createdAt: string;
  cancelledAt?: string;
  cancellationReason?: string;
}

export interface AvailableSlotDto {
  id: string;
  startDateTime: string;
  endDateTime: string;
  clinicianId: string;
  clinicianName: string;
  departmentId: string;
  departmentName: string;
  isAvailable: boolean;
}

export interface RescheduleResponse {
  appointmentId: string;
  oldConfirmationReference: string;
  newConfirmationReference: string;
  oldSlot: AvailableSlotDto;
  newSlot: AvailableSlotDto;
}

export interface CancellationResponse {
  appointmentId: string;
  confirmationReference: string;
  cancelledAt: string;
  refundAmount?: number;
}

export interface ClinicalNoteResponseDto {
  id: string;
  appointmentId: string;
  clinicianId: string;
  clinicianName: string;
  content: string;
  consultationType?: string;
  findings?: string;
  recommendations?: string;
  isPrivate: boolean;
  createdAt: string;
  updatedAt?: string;
  syncedToEhr: boolean;
  syncedAt?: string;
}

export interface SyncToEhrResponse {
  success: boolean;
  ehrResourceId?: string;
  syncedAt: string;
  error?: string;
}

export interface PatientDemographicsDto {
  id: string;
  nhsNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  email: string;
  phoneNumber: string;
  address: string;
  lastUpdated: string;
}

export interface MedicalHistoryDto {
  patientId: string;
  nhsNumber: string;
  diagnoses: DiagnosisDto[];
  chronicConditions: ConditionDto[];
  lastUpdated: string;
}

export interface DiagnosisDto {
  code: string;
  description: string;
  dateRecorded: string;
  status: string;
}

export interface ConditionDto {
  code: string;
  description: string;
  status: string;
}

export interface AllergiesDto {
  patientId: string;
  nhsNumber: string;
  allergies: AllergyDto[];
  lastUpdated: string;
}

export interface AllergyDto {
  substance: string;
  severity: string;
  reaction: string;
  onsetDate: string;
}

export interface MedicationsDto {
  patientId: string;
  nhsNumber: string;
  medications: MedicationDto[];
  lastUpdated: string;
}

export interface MedicationDto {
  code: string;
  name: string;
  dosage: string;
  frequency: string;
  startDate: string;
  prescribedBy: string;
  isActive: boolean;
}

export interface SyncResultDto {
  success: boolean;
  message: string;
  syncedAt: string;
}

export interface HealthCheckDto {
  healthy: boolean;
  message: string;
  checkedAt: string;
}

export interface ReportDataDto {
  summary: ReportSummaryDto;
  details: ReportDetailDto[];
  charts?: ChartDataDto[];
}

export interface ReportSummaryDto {
  totalBookings: number;
  totalCancellations: number;
  totalDna: number;
  averageUtilisation: number;
  byDepartment: DepartmentSummaryDto[];
  byClinician: ClinicianSummaryDto[];
}

export interface ReportDetailDto {
  id: string;
  date: string;
  type: string;
  status: string;
  department: string;
  clinician: string;
  patient?: string;
}

export interface ChartDataDto {
  type: string;
  title: string;
  data: Record<string, unknown>[];
}

export interface DepartmentSummaryDto {
  departmentId: string;
  departmentName: string;
  totalBookings: number;
  totalCancellations: number;
  totalDna: number;
  utilisationRate: number;
}

export interface ClinicianSummaryDto {
  clinicianId: string;
  clinicianName: string;
  totalBookings: number;
  totalCancellations: number;
  totalDna: number;
  utilisationRate: number;
  averageRating?: number;
}

export interface GenerateReportResponse {
  reportId: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  downloadUrl?: string;
  expiresAt: string;
}

export interface ReportResponse {
  reportId: string;
  reportType: string;
  generatedAt: string;
  data: ReportDataDto;
  metadata: ReportMetadataDto;
}

export interface ReportMetadataDto {
  generatedBy: string;
  generatedAt: string;
  dateRange: {
    start: string;
    end: string;
  };
  filters: Record<string, unknown>;
}

export interface ReportTypeDto {
  type: ReportType;
  name: string;
  description: string;
}

export interface SlotSuggestionsResponse {
  suggestions: RankedSlotDto[];
  totalAvailable: number;
}

export interface RankedSlotDto {
  slot: AvailableSlotDto;
  rank: number;
  score: number;
  matchReasons: string[];
}

export interface ClinicianAvailability {
  clinicianId: string;
  regularSchedule: RegularSchedule[];
  leavePeriods: LeavePeriod[];
  slotConfigurations: SlotConfiguration[];
}

export interface GenerateSlotsResponse {
  slotsGenerated: number;
  slotsBlocked: number;
  warnings: string[];
}

export interface ScheduleResponse {
  clinicianId: string;
  viewType: string;
  dateRange: {
    start: string;
    end: string;
  };
  appointments: ScheduledAppointment[];
}

export interface ScheduledAppointment {
  id: string;
  patientId: string;
  patientName: string;
  patientNhsNumber: string;
  startDateTime: string;
  endDateTime: string;
  appointmentType: string;
  status: string;
  ehrFlags: EhrFlag[];
  hasClinicalNotes: boolean;
  isFollowUpRequired: boolean;
}

export interface EhrFlag {
  type: 'Allergy' | 'Medication' | 'Alert';
  severity: 'High' | 'Medium' | 'Low';
  description: string;
}

export interface ErrorResponse {
  type?: string;
  title?: string;
  status: number;
  detail: string;
  errors?: ValidationError[];
  correlationId?: string;
  timestamp?: string;
}

export interface ValidationError {
  field: string;
  message: string;
}

import {
  UserRole,
  ClinicianStatus,
  AppointmentType,
  AppointmentStatus,
  ReportType,
  ReportFormat
} from './enums';
import {
  Address,
  RegularSchedule,
  LeavePeriod,
  SlotConfiguration
} from './entities';
