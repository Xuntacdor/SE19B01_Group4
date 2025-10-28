# Admin, Moderator, Transaction, Upload, Notification Modules Test Prompts

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Admin, Moderator, Transaction, Upload, and Notification modules (.NET 8, xUnit, Moq, FluentAssertions) — handles administrative operations, moderation, transactions, file uploads, and notifications.  
Architecture: Controller → Service → External Services → Repository.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, business logic, or exception handling.  
- List mockable dependencies (e.g., `IUserRepository`, `ITransactionRepository`, `ICloudinaryService`, `IEmailService`).  
- Mention edge cases (null inputs, invalid data, exceptions, authorization checks, file upload limits).  
- Suggest both **happy-path** and **failure** test cases.  

**Output Format (Markdown):**  
```markdown
### Functions to Test
1. **FunctionName(params)**
   - **Main Purpose:** one-line summary  
   - **Inputs:** type + meaning  
   - **Returns:** type + purpose  
   - **Dependencies to Mock:**  
   - **Edge Cases:**  
   - **Suggested Test Names:** Given_When_Then...
```

## Phase 2 – Test Case Matrix

**Context:**  
IELTSPhobic Admin, Moderator, Transaction, Upload, and Notification modules (.NET 8 WebAPI + xUnit) — handles user management, content moderation, payment transactions, media uploads, and notifications.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (AdminController, ModeratorController, TransactionController, UploadController, NotificationController, and their services).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `Cloudinary`, `IUserRepository`, `ITransactionRepository`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid user When upgraded | user data | Returns success |
| Edge Case | Given invalid file When uploaded | invalid format | Returns BadRequest |
| Error | Given unauthorized access | invalid role | Returns Forbidden |
| Integration | Given valid transaction When processed | transaction data | Updates balance |

**Output Title:**  
`### AI Output: Test Cases Matrix for AdminService`

## Phase 3 – Code Generation

### Prompt 1 – AdminController & AdminService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `AdminController` and `AdminService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `AdminController`, mock: `IAdminService`, `ILogger<AdminController>`.
- For `AdminService`, mock: `IUserRepository`, `IExamRepository`, `ITransactionRepository`.
- Test all main public methods:
  - AdminController: `GetAllUsers`, `GetUserById`, `UpdateUser`, `DeleteUser`, `GetDashboardStats`, `GetUserTransactions`
  - AdminService: `GetAllUsers`, `GetUserById`, `UpdateUser`, `DeleteUser`, `GetDashboardStats`, `ModerateContent`
- Test authorization checks (admin role required).
- Include both success and failure scenarios (BadRequest, NotFound, Forbidden).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – ModeratorController & ModeratorService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `ModeratorController` (if exists) and moderation-related services in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock: `IPostRepository`, `ICommentRepository`, `IUserRepository`.
- Test all main public methods:
  - ModeratorController: `GetPendingContent`, `ApproveContent`, `RejectContent`, `DeleteContent`
  - ModeratorService: `GetPendingContent`, `ApproveContent`, `RejectContent`, `DeleteContent`, `BanUser`
- Test content moderation workflows, approval/rejection logic.
- Include edge cases like already moderated content, unauthorized moderation.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – TransactionController & TransactionService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `TransactionController` and `TransactionService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `TransactionController`, mock: `ITransactionService`, `ILogger<TransactionController>`.
- For `TransactionService`, mock: `ITransactionRepository`, `IUserRepository`, `IVipPlanService`.
- Test all main public methods:
  - TransactionController: `GetAll`, `GetById`, `GetByUser`, `GetByDateRange`, `Create`, `Refund`
  - TransactionService: `GetAll`, `GetById`, `GetByUser`, `CreateTransaction`, `ProcessRefund`, `GetTransactionHistory`
- Test transaction creation, refund processing, balance updates.
- Include edge cases like insufficient balance, duplicate transactions.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 4 – UploadController & Upload Service
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `UploadController` and upload-related services in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `Cloudinary` service, `IFormFile`, and `IImageConverter`.
- Test all main public methods:
  - UploadController: `UploadImage`, `UploadAudio`, `UploadVideo`, `DeleteFile`
  - UploadService: `UploadImageAsync`, `UploadAudioAsync`, `UploadVideoAsync`, `DeleteFileAsync`, `ValidateFile`
- Test file validation, upload limits, file type restrictions.
- Include edge cases like oversized files, invalid formats, Cloudinary failures.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 5 – NotificationController & Notification Service
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `NotificationController` and `NotificationService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `NotificationController`, mock: `INotificationService`, `ILogger<NotificationController>`.
- For `NotificationService`, mock: `INotificationRepository`, `IUserRepository`, `IEmailService`.
- Test all main public methods:
  - NotificationController: `GetAll`, `GetById`, `GetUnread`, `MarkAsRead`, `MarkAllAsRead`, `Create`
  - NotificationService: `GetAllNotifications`, `GetUnreadNotifications`, `MarkAsRead`, `CreateNotification`, `SendEmailNotification`
- Test notification creation, read status updates, email sending.
- Include edge cases like duplicate notifications, expired notifications.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 6 – TransactionRepository
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `TransactionRepository` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

Mock dependencies: `ApplicationDbContext` and its `DbSet<Transaction>`.

**Test all main public methods:**
For the repository — `GetAll`, `GetById`, `GetByUser`, `GetByDateRange`, `Add`, `Update`, `Delete`.

Include helper methods such as `CreateSampleTransaction()` and `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in ```csharp ... ``` with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for TransactionRepository.

## Phase 4 – Bug Fixes and Coverage

### Fix Failing Tests
You are an expert .NET test engineer.
I will give you a C# test class and the `dotnet test` output.
Analyze the failing tests and explain why each failed.
Determine whether the issue is in the test expectations or in the controller/service logic.
Then show the exact corrected code snippet for each failed test.

**Format:**
1. Reason for failure
2. Corrected code snippet (test or controller)

### Increase Coverage
I'm testing the AdminService, ModeratorService, TransactionService, and NotificationService classes in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.


