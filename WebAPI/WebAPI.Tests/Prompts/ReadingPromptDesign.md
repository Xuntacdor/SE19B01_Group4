
`Prompt1`
**Context:**  
This code belongs to the IELTSPhobic Reading module (.NET 8 WebAPI), handling reading exams, submissions, scoring, and feedback.  
Architecture: Controller → Service → Repository → Database.
**Role:**  
You are an expert software test engineer and prompt engineer.

**Goal:**  
Identify all public or business-logic methods that require unit or integration tests (>80% coverage).

**Requirements:**  
- Ignore trivial getters/setters.  
- Include controller + service layers with logic, validation, or exception handling.  
- List dependencies to mock (e.g., IReadingRepository, IExamRepository).  
- Mention edge cases (nulls, exceptions, unauthorized, invalid input).  
**Output Format (Markdown):**
### Functions to Test
1. **FunctionName(params)**
   - **Main Purpose:** one-line summary  
   - **Inputs:** list type + meaning  
   - **Returns:** type + purpose  
   - **Dependencies to Mock:**  
   - **Edge Cases:**  
   - **Suggested Test Names:** Given_When_Then...
*Keep each method entry concise but complete enough for automated test generation tools. 
`Promp 2`
**Prompt:**
Analyze the following source code from my GitHub repository (IELTSPhobic project). This code belongs to the **Reading module** built with **.NET 8 WebAPI + xUnit**.
**Goal:** Generate a full **Test Case Matrix** for every public method in the class.

**Instructions:**

1. Analyze the class (e.g. `ReadingService`, `ReadingController`, `ReadingRepository`).
2. Identify **testable functions** (ignore trivial getters/setters).
3. For each function, create **4–6 test cases** under:

   * **Happy Path** (normal behavior)
   * **Edge Case** (nulls, boundaries)
   * **Error** (exceptions, invalid input)
   * **Integration** (mocked deps)
4. Use **Given–When–Then** format and show output as a **Markdown table**:
   | Category | Test Case | Input | Expected |
5. Ensure >80% line + branch coverage and mention any **mocked dependencies** (e.g. `IReadingRepository`, `IExamRepository`).

**Output Example:**

| Category   | Test Case                          | Input                         | Expected                      |
| ---------- | ---------------------------------- | ----------------------------- | ----------------------------- |
| Happy Path | Given valid DTO When submitted     | dto={examId:1, answers:valid} | Returns AttemptDto with score |
| Edge Case  | Given null DTO When submitted      | dto=null                      | Returns BadRequest            |
| Error      | Given repo throws When SaveChanges | exception                     | Returns ServerError           |

**Output title:**
`### AI Output: Test Cases Matrix for ReadingService`
`Prompt 3`
**Context:**  
IELTSPhobic ReadingService (.NET 8, xUnit, Moq, FluentAssertions).

**Role:**  
Expert .NET test engineer.

**Goal:**  
Generate fully compilable unit test code covering: GetById, GetByExam, GetAll, Add, Update, Delete, EvaluateReading.

**Requirements:**  
- Namespace: WebAPI.Tests  
- Mock: IReadingRepository, IExamService  
- Include helpers: CreateSampleReading(), SetupMockRepository()  
- Each [Fact] = standalone test  
- Output = single C# markdown block (no comments)

**Output Format:**
// full C# source code here

`promp4`
You are an expert .NET test engineer.
Generate fully compilable **xUnit test code (C#)** for the **ReadingController** and **ReadingRepository** classes in the IELTSPhobic web application.
Do not create or save any file — only output the full C# source code as markdown.

Namespace must be `WebAPI.Tests`.
Use **xUnit**, **Moq**, and **FluentAssertions**.

For **ReadingController**, mock dependencies: `IReadingService`, `IExamService`, and `ILogger<ReadingController>`.
For **ReadingRepository**, mock dependencies: `ApplicationDbContext` and its `DbSet<Reading>`.

Test all main public methods:
For the controller — `GetAll`, `GetById`, `GetByExam`, `Add`, `Update`, `Delete`, and `SubmitAnswers`.
For the repository — `GetAll`, `GetById`, `GetByExamId`, `Add`, `Update`, and `Delete`.

Include helper methods such as `CreateSampleReading()` and `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Ensure all JSON strings are properly escaped.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in
`csharp ... `
with no extra commentary.

Goal: produce clean, syntactically correct, and high-coverage test code that compiles successfully for both ReadingController and ReadingRepository.
`Fix`
You are an expert .NET test engineer.
I will give you a C# test class and the `dotnet test` output.
Analyze the failing tests and explain why each failed.
Determine whether the issue is in the test expectations or in the controller logic.
Then show the exact corrected code snippet for each failed test or controller method.
Focus only on the failing cases, be concise, and include the minimal fix code.

Format:

1. Reason for failure
2. Corrected code snippet (test or controller)

Input will include:

```
--- Test Class ---
<code>

--- Test Output ---
<dotnet test output>
```
