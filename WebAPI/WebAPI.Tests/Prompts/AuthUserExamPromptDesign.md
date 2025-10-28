# Auth, User, and Exam Modules Test Prompts

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Authentication, User Management, and Exam modules (.NET 8, xUnit, Moq, FluentAssertions) — handles user authentication, profile management, exam CRUD operations, and attempt tracking.  
Architecture: Controller → Service → Repository → Database.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, business logic, or exception handling.  
- List mockable dependencies (e.g., `IUserRepository`, `IExamRepository`, `IExamAttemptRepository`, `IOtpService`, `IPasswordService`, `ITransactionService`).  
- Mention edge cases (null inputs, invalid data, exceptions, unauthorized access, duplicate registrations).  
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
IELTSPhobic Auth, User, Exam modules (.NET 8 WebAPI + xUnit) — handles login, registration, password reset, user profiles, exam management, and attempt submission.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (`AuthController`, `UserController`, `ExamController`, `AuthService`, `UserService`, `ExamService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `IUserRepository`, `IExamRepository`, `IOtpService`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid credentials When login | email:valid, password:valid | Returns JWT token |
| Edge Case | Given null password When login | password=null | Returns BadRequest |
| Error | Given invalid credentials | wrong password | Returns Unauthorized |
| Integration | Given valid user When created | user data | Returns created user |

**Output Title:**  
`### AI Output: Test Cases Matrix for AuthService`

## Phase 3 – Code Generation

### Prompt 1 – AuthController & AuthService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `AuthController` and `AuthService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `AuthController`, mock: `IAuthService`, `ILogger<AuthController>`, `IUserService`.
- For `AuthService`, mock: `IUserRepository`, `IOtpService`, `IPasswordService`, `ITransactionService`, `IEmailService`.
- Test all main public methods:
  - AuthController: `Register`, `Login`, `LoginGoogle`, `Logout`, `ForgotPassword`, `ResetPassword`, `RefreshToken`
  - AuthService: `RegisterAsync`, `LoginAsync`, `ValidateUser`, `GenerateJwtToken`, `RefreshTokenAsync`
- Include helper methods like `CreateSampleUser()`, `SetupUserRepository()`.
- Test scenarios for duplicate email, invalid credentials, expired OTP, etc.
- Use realistic data and assertions that fully compile.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – UserController & UserService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `UserController` and `UserService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `UserController`, mock: `IUserService`, `ILogger<UserController>`.
- For `UserService`, mock: `IUserRepository`, `ITransactionService`, `ISignInHistoryService`.
- Test all main public methods:
  - UserController: `GetProfile`, `UpdateProfile`, `GetUserStats`, `GetTransactionHistory`, `GetSignInHistory`
  - UserService: `GetById`, `GetProfile`, `UpdateProfile`, `GetUserStats`, `GetTransactionHistory`
- Test authorization checks (Unauthorized users).
- Include both success and failure scenarios (BadRequest, NotFound, Exception).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – ExamController & ExamService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `ExamController` and `ExamService` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- For `ExamController`, mock: `IExamService`, `ILogger<ExamController>`.
- For `ExamService`, mock: `IExamRepository`, `IExamAttemptRepository`, `IUserRepository`.
- Test all main public methods:
  - ExamController: `GetAll`, `GetById`, `GetByType`, `Create`, `Update`, `Delete`, `StartExam`, `GetUserAttempts`
  - ExamService: `GetAll`, `GetById`, `GetByType`, `Create`, `Update`, `Delete`, `SubmitAttempt`, `GetUserAttempts`
- Test exam attempt tracking, scoring, and feedback storage.
- Include edge cases like duplicate submissions, missing exams, invalid data.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 4 – UserRepository & ExamRepository
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `UserRepository`, `ExamRepository`, and `ExamAttemptRepository` classes in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

Mock dependencies: `ApplicationDbContext` and its `DbSet<User>`, `DbSet<Exam>`, `DbSet<ExamAttempt>`.

**Test all main public methods:**
For the repositories — `GetAll`, `GetById`, `Add`, `Update`, `Delete`, `GetByEmail`, `GetByExamType`, etc.

Include helper methods such as `CreateSampleUser()`, `CreateSampleExam()`, `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in ```csharp ... ``` with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for all repositories.

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
I'm testing the AuthService, UserService, and ExamService classes in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to the test files.


