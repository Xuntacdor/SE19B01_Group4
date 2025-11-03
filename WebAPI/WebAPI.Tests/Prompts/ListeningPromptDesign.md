
# Prompt 1 – Listening Module Test Identification

**Context:**  
IELTSPhobic web app — **Listening Module** (exam management, question retrieval, user submissions, scoring, feedback).  
Architecture: **Controller → Service → Repository → Database**

**Role:**  
Expert software test engineer and prompt engineer.

**Goal:**  
Identify all **public/business-logic methods** that need **unit or integration testing** to achieve **>80% coverage**.

**Requirements:**  
- Focus on logic in Controller & Service layers.  
- Ignore trivial getters/setters or mapping helpers.  
- Include validation, authorization, and exception branches.  
- List mockable dependencies (e.g., `IListeningRepository`, `IExamRepository`).  
- Cover happy path + failure (nulls, invalid DTO, DB exception, not found, unauthorized).  

**Output Format:**  
```markdown
### Functions to Test
1. **FunctionName(params)**
   - **Main Purpose:** One-line summary.  
   - **Inputs:** Type + meaning.  
   - **Returns:** Data type + meaning.  
   - **Dependencies to Mock:** Services, repositories, or APIs.  
   - **Edge Cases:** Invalid inputs, nulls, exceptions, missing records.  
   - **Suggested Test Names:** Given_When_Then pattern.```

`Promp 2`
**Prompt:**
Analyze the following source code from my GitHub repository (IELTSPhobic project). This code belongs to the **Listening module** built with **.NET 8 WebAPI + xUnit**.
**Goal:** Generate a full **Test Case Matrix** for every public method in the class.

**Instructions:**

1. Analyze the class (e.g. `ListeningService`, `ListeningController`, `ListeningRepository`).
2. Identify **testable functions** (ignore trivial getters/setters).
3. For each function, create **4–6 test cases** under:

   * **Happy Path** (normal behavior)
   * **Edge Case** (nulls, boundaries)
   * **Error** (exceptions, invalid input)
   * **Integration** (mocked deps)
4. Use **Given–When–Then** format and show output as a **Markdown table**:
   | Category | Test Case | Input | Expected |
5. Ensure >80% line + branch coverage and mention any **mocked dependencies** (e.g. `IListeningRepository`, `IExamRepository`).

**Output Example:**

| Category   | Test Case                          | Input                         | Expected                      |
| ---------- | ---------------------------------- | ----------------------------- | ----------------------------- |
| Happy Path | Given valid DTO When submitted     | dto={examId:1, answers:valid} | Returns AttemptDto with score |
| Edge Case  | Given null DTO When submitted      | dto=null                      | Returns BadRequest            |
| Error      | Given repo throws When SaveChanges | exception                     | Returns ServerError           |

**Output title:**
`### AI Output: Test Cases Matrix for ListeningService`
`Prompt 3`
You are an expert .NET test engineer. Generate fully compilable xUnit test code (C#) for the ListeningService class in the IELTSPhobic web application.
Do not create or save any file — only output the full C# source code as markdown.

Requirements:

Namespace: WebAPI.Tests

Use xUnit, Moq, and FluentAssertions.

Mock dependencies: IListeningRepository, IExamService.

Test all main public methods:
GetById, GetByExam, GetAll, Add, Update, Delete, and EvaluateListening.

Include helper methods like CreateSampleListening() and SetupMockRepository().

Use realistic data and assertions that fully compile under .NET 8.

Ensure all JSON strings are properly escaped.

Each [Fact] must be a standalone public test method.

The output must be a single C# code block wrapped in
csharp ...
with no extra commentary.

Goal: Produce clean, syntactically correct test code that compiles and covers all ListeningService logic.
`promp4`
You are an expert .NET test engineer.
Generate fully compilable **xUnit test code (C#)** for the **ListeningController** and **ListeningRepository** classes in the IELTSPhobic web application.
Do not create or save any file — only output the full C# source code as markdown.

Namespace must be `WebAPI.Tests`.
Use **xUnit**, **Moq**, and **FluentAssertions**.

For **ListeningController**, mock dependencies: `IListeningService`, `IExamService`, and `ILogger<ListeningController>`.
For **ListeningRepository**, mock dependencies: `ApplicationDbContext` and its `DbSet<Listening>`.

Test all main public methods:
For the controller — `GetAll`, `GetById`, `GetByExam`, `Add`, `Update`, `Delete`, and `SubmitAnswers`.
For the repository — `GetAll`, `GetById`, `GetByExamId`, `Add`, `Update`, and `Delete`.

Include helper methods such as `CreateSampleListening()` and `SetupMockRepository()`.
Use realistic data and assertions that fully compile under **.NET 8**.
Ensure all JSON strings are properly escaped.
Each `[Fact]` must be a standalone public test method.

The output must be a single **C# code block** wrapped in
`csharp ... `
with no extra commentary.

Goal: produce clean, syntactically correct, and high-coverage test code that compiles successfully for both ListeningController and ListeningRepository.
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
