# Data Flow Diagram - DOCX Extractor

## System Architecture Flow

```mermaid
graph TD
    Start([Start Application]) --> Init[Initialize Database]
    Init --> CreateProfiles[Create/Update Filter Profiles]
    CreateProfiles --> CreateTemplate[Step 1: Create Sample Template]
    
    CreateTemplate --> FillTemplate[Step 2: Fill Template with Sample Data]
    FillTemplate --> ExtractFields[Step 3: Extract Fields from DOCX]
    
    ExtractFields --> FieldList["📋 Extracted Fields<br/>7 fields total"]
    
    FieldList --> Validate[Step 4: Validate Field Values]
    Validate --> ValidationCheck{Field Valid?}
    
    ValidationCheck -->|Valid| ValidField["✓ Keep original value"]
    ValidationCheck -->|Invalid| InvalidField["✗ Append error message"]
    
    ValidField --> FilterStep[Step 5: Apply Filter Profile]
    InvalidField --> FilterStep
    
    FilterStep --> GetFilter["Load Active Filter Profile<br/>from PostgreSQL"]
    GetFilter --> FilterCheck{Field Type<br/>in Include List?}
    
    FilterCheck -->|No Include Rules| IncludeAll["Include all fields"]
    FilterCheck -->|Yes, Field Matches| Include["Include field"]
    FilterCheck -->|No Match| Exclude["Exclude field"]
    
    IncludeAll --> CheckExclude{Field Type<br/>in Exclude List?}
    Include --> CheckExclude
    Exclude --> ExcludeResult["Skip field"]
    
    CheckExclude -->|Yes, Field Matches| ExcludeResult
    CheckExclude -->|No Match| FilteredList["Filtered Fields"]
    
    ExcludeResult --> FilteredList
    
    FilteredList --> JsonOutput[Step 6: Generate JSON Output]
    JsonOutput --> JsonFile["📄 extracted-fields.json"]
    
    JsonFile --> TableOutput[Step 7: Generate Table Output]
    TableOutput --> TableDisplay["📊 Formatted Table"]
    
    TableDisplay --> SaveDB[Step 8: Save to PostgreSQL]
    SaveDB --> ExtractedFieldsTable["🗄️ ExtractedFields Table<br/>Columns: Id, DocumentName,<br/>FilterProfileName, FieldKey,<br/>FieldLabel, FieldValue,<br/>FieldType, IsValid,<br/>ValidationMessage,<br/>ExtractedAt"]
    
    ExtractedFieldsTable --> Report[Step 9: Generate Report]
    Report --> SummaryReport["📊 Summary Report<br/>• Total fields<br/>• Valid/Invalid count<br/>• Fields by type<br/>• Error summary"]
    
    SummaryReport --> Complete([Complete ✓])
    
    style Start fill:#e1f5e1
    style Complete fill:#e1f5e1
    style FieldList fill:#fff4e6
    style FilteredList fill:#fff4e6
    style ExtractedFieldsTable fill:#e3f2fd
    style SummaryReport fill:#f3e5f5
    style ValidField fill:#c8e6c9
    style InvalidField fill:#ffcdd2
    style Include fill:#c8e6c9
    style Exclude fill:#ffcdd2
```

## Data Transformation Pipeline

```mermaid
graph LR
    subgraph Input["📥 Input"]
        Template["Word Template<br/>.docx file"]
    end
    
    subgraph Processing["⚙️ Processing"]
        Extract["Content Control<br/>Extraction"]
        Validate["Field<br/>Validation"]
        Filter["Filter<br/>Application"]
    end
    
    subgraph Database["🗄️ PostgreSQL Database"]
        Profiles["FilterProfiles<br/>Table"]
        Rules["FilterRules<br/>Table"]
        Extracted["ExtractedFields<br/>Table"]
    end
    
    subgraph Output["📤 Output"]
        JSON["JSON<br/>File"]
        Table["Table<br/>Display"]
        Report["Summary<br/>Report"]
    end
    
    Template --> Extract
    Extract --> Validate
    Validate --> Filter
    
    Profiles -.->|Load Active Profile| Filter
    Rules -.->|Include/Exclude Rules| Filter
    
    Filter --> Extracted
    Extracted --> JSON
    Extracted --> Table
    Extracted --> Report
    
    style Input fill:#e8f5e9
    style Processing fill:#fff3e0
    style Database fill:#e3f2fd
    style Output fill:#f3e5f5
```

## Filter Logic Decision Tree

```mermaid
graph TD
    Start["Field Type Detected<br/>e.g., 'Email'"] --> CheckProfile["Load Active<br/>Filter Profile"]
    
    CheckProfile --> HasInclude{Profile has<br/>Include Rules?}
    
    HasInclude -->|No Include Rules| AllowByDefault["✓ Allow field<br/>by default"]
    
    HasInclude -->|Yes Include Rules| CheckIn{Field Type<br/>in Include<br/>List?}
    
    CheckIn -->|No| Reject1["✗ Reject field<br/>Not in include list"]
    CheckIn -->|Yes| CheckExclude{Field Type<br/>in Exclude<br/>List?}
    
    AllowByDefault --> CheckExclude
    
    CheckExclude -->|Yes| Reject2["✗ Reject field<br/>In exclude list"]
    CheckExclude -->|No| Accept["✓ Include field<br/>in output"]
    
    Reject1 --> End["Field Filtered Out"]
    Reject2 --> End
    Accept --> Save["Save to<br/>PostgreSQL"]
    
    style Start fill:#fff9c4
    style Accept fill:#c8e6c9
    style Reject1 fill:#ffcdd2
    style Reject2 fill:#ffcdd2
    style Save fill:#bbdefb
```

## Sample Filter Profiles

```mermaid
graph TB
    subgraph contact_info["contact_info Profile"]
        A["INCLUDE: Email, Phone"]
        B["EXCLUDE: (none)"]
        A --> Result1["Result: Only Email/Phone fields"]
    end
    
    subgraph no_sensitive["no_sensitive Profile"]
        C["INCLUDE: (none - all by default)"]
        D["EXCLUDE: Email, Phone"]
        C --> Result2["Result: All fields except Email/Phone"]
    end
    
    subgraph dates_only["dates_only Profile"]
        E["INCLUDE: Date"]
        F["EXCLUDE: (none)"]
        E --> Result3["Result: Only Date fields"]
    end
    
    style contact_info fill:#e8f5e9
    style no_sensitive fill:#fce4ec
    style dates_only fill:#e1f5fe
```

## Database Schema Relationships

```mermaid
erDiagram
    FILTERPROFILES ||--o{ FILTERRULES : contains
    FILTERPROFILES ||--o{ EXTRACTEDFIELDS : "filtered by"
    
    FILTERPROFILES {
        int Id PK
        string Name UK
        string Description
        boolean IsActive
        timestamp CreatedAt
        timestamp UpdatedAt
    }
    
    FILTERRULES {
        int Id PK
        int FilterProfileId FK
        string FieldType
        boolean IsIncludeRule
        timestamp CreatedAt
    }
    
    EXTRACTEDFIELDS {
        int Id PK
        string DocumentName IX
        string FilterProfileName IX
        string FieldKey
        string FieldLabel
        string FieldValue
        string FieldType IX
        boolean IsValid IX
        string ValidationMessage
        timestamp ExtractedAt IX
    }
```

## Field Validation Rules

```mermaid
graph TD
    FieldValue["Field Value"] --> Detect["Detect Field Type<br/>from key/label"]
    
    Detect --> Email{Type:<br/>Email?}
    Email -->|Yes| EmailRegex["Regex: RFC email format<br/>user@domain.com"]
    EmailRegex --> EmailValid{Matches?}
    EmailValid -->|Yes| Valid1["✓ Valid"]
    EmailValid -->|No| Invalid1["✗ Invalid: invalid email format"]
    
    Email -->|No| Phone{Type:<br/>Phone?}
    Phone -->|Yes| PhoneRegex["Regex: 10+ digits<br/>with formatting"]
    PhoneRegex --> PhoneValid{Valid?}
    PhoneValid -->|Yes| Valid2["✓ Valid"]
    PhoneValid -->|No| Invalid2["✗ Invalid: invalid phone format"]
    
    Phone -->|No| Date{Type:<br/>Date?}
    Date -->|Yes| DateParse["Parse: YYYY-MM-DD<br/>MM/DD/YYYY, etc"]
    DateParse --> DateValid{Valid Date?}
    DateValid -->|Yes| Valid3["✓ Valid"]
    DateValid -->|No| Invalid3["✗ Invalid: invalid date format"]
    
    Date -->|No| Other["Type: Text/Other"]
    Other --> Valid4["✓ Valid<br/>Any non-empty string"]
    
    Valid1 --> Output["Output Field<br/>with validation status"]
    Valid2 --> Output
    Valid3 --> Output
    Valid4 --> Output
    Invalid1 --> Output
    Invalid2 --> Output
    Invalid3 --> Output
    
    style Valid1 fill:#c8e6c9
    style Valid2 fill:#c8e6c9
    style Valid3 fill:#c8e6c9
    style Valid4 fill:#c8e6c9
    style Invalid1 fill:#ffcdd2
    style Invalid2 fill:#ffcdd2
    style Invalid3 fill:#ffcdd2
```

## Execution Timeline

```mermaid
sequenceDiagram
    participant User
    participant App as DocxExtractor
    participant DB as PostgreSQL
    participant File as DOCX File
    
    User->>App: Run Application
    App->>DB: Initialize Database
    App->>DB: Load Filter Profiles
    DB-->>App: Active Profile (contact_info)
    
    App->>File: Create Template
    App->>File: Fill with Sample Data
    File-->>App: Updated DOCX
    
    App->>File: Extract Content Controls
    File-->>App: 7 Fields Extracted
    
    App->>App: Validate Each Field
    App->>App: Apply Filters
    Note over App: contact_info = Email & Phone only
    App-->>App: 1 Field After Filtering
    
    App->>File: Save as extracted-fields.json
    
    App->>DB: Save to ExtractedFields Table
    DB-->>App: Confirmation
    
    App->>DB: Query Summary Report
    DB-->>App: Report Data
    
    App->>User: Display Results
```

## Performance Considerations

```mermaid
graph TD
    Input["7 Input Fields"] --> Extract["Extraction<br/>~1ms"]
    Extract --> Validate["Validation<br/>~2ms<br/>regex matching"]
    Validate --> Filter["Filtering<br/>~0.5ms<br/>list lookup"]
    
    Filter --> Output["1 Output Field<br/>~0.5ms overhead"]
    
    Output --> DB["Save to PostgreSQL<br/>~10-50ms<br/>network latency"]
    
    DB --> Report["Generate Report<br/>~5ms<br/>SQL query"]
    
    Report --> Total["Total: ~70ms<br/>mostly DB operations"]
    
    style Extract fill:#fff9c4
    style Validate fill:#fff9c4
    style Filter fill:#fff9c4
    style DB fill:#bbdefb
    style Report fill:#bbdefb
```
