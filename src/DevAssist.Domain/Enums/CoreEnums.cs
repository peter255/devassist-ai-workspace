namespace DevAssist.Domain.Enums;

public enum DocumentStatus
{
    Uploaded = 1,
    Processing = 2,
    Indexed = 3,
    Failed = 4
}

public enum DocumentType
{
    EngineeringSpecification = 1,
    ArchitectureDecisionRecord = 2,
    IncidentPostmortem = 3,
    Runbook = 4,
    TicketAttachment = 5,
    RequirementDocument = 6,
    Other = 99
}

public enum ChatMessageRole
{
    User = 1,
    Assistant = 2,
    System = 3
}

public enum TicketSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
