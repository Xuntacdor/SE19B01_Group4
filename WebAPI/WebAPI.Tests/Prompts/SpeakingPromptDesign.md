# Speaking Module Test Prompts

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic Speaking module (.NET 8, xUnit, Moq, FluentAssertions) — handles speaking exams with AI-powered speech recognition and feedback.  
Architecture: Controller → Service → External AI (OpenAI) → Repository.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, AI interaction, or exception handling.  
- List mockable dependencies (e.g., `ISpeakingRepository`, `ISpeakingFeedbackRepository`, `IOpenAIService`, `IExamService`, `ISpeechToTextService`).  
- Mention edge cases (null inputs, invalid data, exceptions, unauthorized, 500 errors).  
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
IELTSPhobic Speaking module (.NET 8 WebAPI + xUnit) — uses OpenAI and Speech-to-Text for audio transcription and grading.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze one class (`SpeakingService`, `SpeakingController`, `SpeakingRepository`, `SpeakingFeedbackService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `OpenAIService`, `ISpeechToTextService`, `ISpeakingRepository`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid audio When submitted | dto={examId:1, audio:"base64"} | Returns graded feedback |
| Edge Case | Given null audio When submitted | dto=null | Returns BadRequest |
| Error | Given AI throws error | mock throws | Returns 500 |
| Integration | Given valid audio When graded | mocks active | Calls services once |

**Output Title:**  
`### AI Output: Test Cases Matrix for SpeakingService`

## Phase 3 – Code Generation

### Prompt 1 – SpeakingService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `SpeakingService` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock dependencies: `ISpeakingRepository`, `ISpeakingFeedbackRepository`, `IOpenAIService`, `ISpeechToTextService`, and `IExamService`.
- Test all main public methods: `GetById`, `GetByExam`, `Create`, `Update`, `Delete`, `GradeSpeaking`.
- Include helper methods like `CreateSampleSpeaking()`, `CreateMockAudioResponse()`, and `SetupSaveFeedback()`.
- Use realistic data and assertions that fully compile.
- Ensure JSON strings are properly escaped.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – SpeakingController
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `SpeakingController` class in the IELTSPhobic web application.  
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock dependencies: `ISpeakingService` (the controller uses this service).
- Test all main public methods: `GetById`, `GetByExam`, `Create`, `Update`, `Delete`, `SubmitSpeaking`, `UploadAudio`.
- Include both success and failure (BadRequest, NotFound, Exception) scenarios.
- Use realistic data and assertions that fully compile.
- Ensure JSON and DTOs are properly structured.
- Each `[Fact]` must be a standalone public test method.
- Output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**  
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – SpeakingRepository
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `SpeakingRepository` and `SpeakingFeedbackRepository` classes in the IELTSPhobic web application.  
Do **not** create or save any file — only output the full C# source code as markdown.

**Namespace must be `WebAPI.Tests`.**
Use **xUnit**, **Moq**, and **FluentAssertions**.

For **SpeakingRepository**, mock dependencies: `ApplicationDbContext` and its `DbSet<Speaking>`.
For **SpeakingFeedbackRepository**, mock dependencies: `ApplicationDbContext` and its `DbSet<SpeakingFeedback>`.

**Test all main public methods:**
For the repositories — `GetAll`, `GetById`, `GetByExamId`, `Add`, `Update`, and `Delete`.

Include helper methods such as `CreateSampleSpeaking()` and `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Ensure all JSON strings are properly escaped.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in
```csharp ... ```
with no extra commentary.

**Goal:** Produce clean, syntactically correct, and high-coverage test code that compiles successfully for both SpeakingRepository and SpeakingFeedbackRepository.

## Phase 4 – Bug Fixes and Coverage

### Fix Failing Tests
You are an expert .NET test engineer.
I will give you a C# test class and the `dotnet test` output.
Analyze the failing tests and explain why each failed.
Determine whether the issue is in the test expectations or in the controller/service logic.
Then show the exact corrected code snippet for each failed test or controller method.
Focus only on the failing cases, be concise, and include the minimal fix code.

**Format:**
1. Reason for failure
2. Corrected code snippet (test or controller)

### Increase Coverage
I'm testing the SpeakingService class in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the SpeakingServiceTests.cs file passes all test cases, but the code coverage is only around 75%.  
Please analyze all logical branches in SpeakingService (GetById, Delete, Update, GradeSpeaking, SubmitSpeaking, and SaveFeedback) and generate 5–7 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to SpeakingServiceTests.cs.


