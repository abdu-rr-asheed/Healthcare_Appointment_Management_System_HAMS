export enum UserRole {
  Patient = 'Patient',
  Clinician = 'Clinician',
  Administrator = 'Administrator'
}

export enum ClinicianStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  OnLeave = 'OnLeave'
}

export enum AppointmentType {
  InitialConsultation = 'InitialConsultation',
  FollowUp = 'FollowUp',
  Emergency = 'Emergency'
}

export enum AppointmentStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Cancelled = 'Cancelled',
  Completed = 'Completed',
  DidNotAttend = 'DidNotAttend'
}

export enum NotificationType {
  AppointmentReminder = 'AppointmentReminder',
  BookingConfirmation = 'BookingConfirmation',
  Cancellation = 'Cancellation',
  Reschedule = 'Reschedule',
  System = 'System'
}

export enum LeaveType {
  Annual = 'Annual',
  Sick = 'Sick',
  Study = 'Study',
  Other = 'Other'
}

export enum ReportType {
  BookingSummary = 'BookingSummary',
  CancellationAnalysis = 'CancellationAnalysis',
  DnaReport = 'DnaReport',
  SlotUtilisation = 'SlotUtilisation',
  Custom = 'Custom'
}

export enum ReportFormat {
  CSV = 'CSV',
  PDF = 'PDF'
}

export enum TimeOfDay {
  Morning = 'Morning',
  Afternoon = 'Afternoon',
  Evening = 'Evening',
  Any = 'Any'
}

export enum ReminderType {
  FortyEightHours = 'FortyEightHours',
  TwoHours = 'TwoHours'
}
