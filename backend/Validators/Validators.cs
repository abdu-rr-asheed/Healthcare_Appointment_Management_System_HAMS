using FluentValidation;
using HAMS.API.Models.DTOs.Requests;

namespace HAMS.API.Validators
{
    public class AppointmentRequestValidator : AbstractValidator<AppointmentRequest>
    {
        public AppointmentRequestValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty()
                .WithMessage("Patient ID is required.");

            RuleFor(x => x.ClinicianId)
                .NotEmpty()
                .WithMessage("Clinician ID is required.");

            RuleFor(x => x.AppointmentDate)
                .NotEmpty()
                .WithMessage("Appointment date is required.")
                .GreaterThan(DateTime.Now.AddHours(-1))
                .WithMessage("Appointment cannot be scheduled in the past.");

            RuleFor(x => x.AppointmentType)
                .NotEmpty()
                .WithMessage("Appointment type is required.")
                .MaximumLength(100)
                .WithMessage("Appointment type must not exceed 100 characters.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.");
        }
    }

    public class RescheduleRequestValidator : AbstractValidator<RescheduleRequest>
    {
        public RescheduleRequestValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Appointment ID is required.");

            RuleFor(x => x.NewSlotId)
                .NotEmpty()
                .WithMessage("New slot ID is required.");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.");
        }
    }

    public class CancellationRequestValidator : AbstractValidator<CancellationRequest>
    {
        public CancellationRequestValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Appointment ID is required.");

            RuleFor(x => x.CancellationReason)
                .MaximumLength(500)
                .WithMessage("Cancellation reason must not exceed 500 characters.");

            RuleFor(x => x.CancellationReason)
                .NotEmpty()
                .When(x => x.IsPatientCancellation == false)
                .WithMessage("Cancellation reason is required for clinical cancellations.");
        }
    }

    public class ClinicalNoteRequestValidator : AbstractValidator<ClinicalNoteRequest>
    {
        public ClinicalNoteRequestValidator()
        {
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Appointment ID is required.");

            RuleFor(x => x.NoteContent)
                .NotEmpty()
                .WithMessage("Note content is required.")
                .MaximumLength(5000)
                .WithMessage("Note content must not exceed 5000 characters.");

            RuleFor(x => x.NoteType)
                .NotEmpty()
                .WithMessage("Note type is required.")
                .Must(type => new[] { "Consultation", "FollowUp", "Prescription", "Referral", "General" }.Contains(type))
                .WithMessage("Invalid note type.");
        }
    }

    public class PatientRegistrationRequestValidator : AbstractValidator<PatientRegistrationRequest>
    {
        public PatientRegistrationRequestValidator()
        {
            RuleFor(x => x.NhsNumber)
                .NotEmpty()
                .WithMessage("NHS number is required.")
                .Length(10)
                .WithMessage("NHS number must be exactly 10 digits.")
                .Matches(@"^\d{10}$")
                .WithMessage("NHS number must contain only digits.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.")
                .MaximumLength(100)
                .WithMessage("First name must not exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .MaximumLength(100)
                .WithMessage("Last name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required.")
                .Matches(@"^\+?[\d\s\-()]{10,20}$")
                .WithMessage("Invalid phone number format.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("Date of birth is required.")
                .LessThan(DateTime.Now)
                .WithMessage("Date of birth must be in the past.");

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("Address must not exceed 200 characters.");

            RuleFor(x => x.Postcode)
                .MaximumLength(20)
                .WithMessage("Postcode must not exceed 20 characters.");
        }
    }

    public class ClinicianRegistrationRequestValidator : AbstractValidator<ClinicianRegistrationRequest>
    {
        public ClinicianRegistrationRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.")
                .MaximumLength(100)
                .WithMessage("First name must not exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .MaximumLength(100)
                .WithMessage("Last name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.");

            RuleFor(x => x.Specialty)
                .NotEmpty()
                .WithMessage("Specialty is required.")
                .MaximumLength(100)
                .WithMessage("Specialty must not exceed 100 characters.");

            RuleFor(x => x.JobTitle)
                .MaximumLength(100)
                .WithMessage("Job title must not exceed 100 characters.");

            RuleFor(x => x.GmcNumber)
                .NotEmpty()
                .WithMessage("GMC number is required.")
                .Length(7)
                .WithMessage("GMC number must be exactly 7 digits.")
                .Matches(@"^\d{7}$")
                .WithMessage("GMC number must contain only digits.");
        }
    }

    public class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
    {
        public UpdatePatientRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[\d\s\-()]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Invalid phone number format.");

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("Address must not exceed 200 characters.");

            RuleFor(x => x.Postcode)
                .MaximumLength(20)
                .WithMessage("Postcode must not exceed 20 characters.");

            RuleFor(x => x.EmergencyContactName)
                .MaximumLength(100)
                .WithMessage("Emergency contact name must not exceed 100 characters.");

            RuleFor(x => x.EmergencyContactPhone)
                .Matches(@"^\+?[\d\s\-()]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.EmergencyContactPhone))
                .WithMessage("Invalid emergency contact phone number format.");
        }
    }

    public class SlotGenerationRequestValidator : AbstractValidator<GenerateSlotsRequest>
    {
        public SlotGenerationRequestValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.")
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Start date cannot be in the past.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date.");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= 90)
                .WithMessage("Slot generation period cannot exceed 90 days.");
        }
    }

    public class ReportRequestValidator : AbstractValidator<ReportRequest>
    {
        public ReportRequestValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date.");

            RuleFor(x => x.ReportType)
                .NotEmpty()
                .WithMessage("Report type is required.")
                .Must(type => new[] { "appointments", "performance", "utilization" }.Contains(type.ToLower()))
                .WithMessage("Invalid report type.");
        }
    }
}