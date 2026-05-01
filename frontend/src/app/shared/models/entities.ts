import { UserRole, ClinicianStatus, AppointmentType, AppointmentStatus, NotificationType, LeaveType } from './enums';

export interface User {
  id: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
  firstName: string;
  lastName: string;
  createdAt: string;
}

export interface Patient {
  id: string;
  userId: string;
  nhsNumber: string;
  dateOfBirth: string;
  address: Address;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export interface Clinician {
  id: string;
  userId: string;
  departmentId: string;
  specialty: string;
  qualifications: string[];
  licenseNumber: string;
  status: ClinicianStatus;
  createdAt: string;
}

export interface Department {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export interface Address {
  line1: string;
  line2?: string;
  city: string;
  postalCode: string;
  country: string;
}

export interface Appointment {
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

export interface AvailabilitySlot {
  id: string;
  startDateTime: string;
  endDateTime: string;
  clinicianId: string;
  clinicianName: string;
  departmentId: string;
  departmentName: string;
  isAvailable: boolean;
  isCancelled: boolean;
  createdAt: string;
}

export interface ClinicalNote {
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

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  userRole: string;
  action: string;
  resourceType: string;
  resourceId: string;
  ipAddress: string;
  userAgent: string;
  details: Record<string, unknown>;
  outcome: 'Success' | 'Failure';
}

export interface Notification {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}

export interface RegularSchedule {
  id: string;
  clinicianId: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  recurring: boolean;
}

export interface LeavePeriod {
  id: string;
  clinicianId: string;
  startDate: string;
  endDate: string;
  reason: string;
  type: LeaveType;
}

export interface SlotConfiguration {
  id: string;
  clinicianId: string;
  appointmentType: string;
  durationMinutes: number;
  bufferMinutes: number;
}

export interface RefreshToken {
  id: string;
  token: string;
  userId: string;
  expiresAt: string;
  createdAt: string;
}
