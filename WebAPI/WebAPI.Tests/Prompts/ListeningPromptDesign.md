
`Prompt1`
Context: This source code belongs to the IELTSPhobic web application — specifically, the Listening module, which manages listening exams, question retrieval, user submissions, scoring, and feedback storage.
The system follows a multi-layer architecture:
Controller → Service → Repository → Database 
Your goal is to identify all **public or business-logic methods** that require **unit or integration testing** for this feature.

Requirements:

* You are an **expert software test engineer and prompt engineer**.
* The analysis must be **comprehensive enough to achieve >80% code and branch coverage**.
* Automatically analyze the given source code (you already have GitHub access).
* Ignore trivial getters/setters or mapping helpers without logic.
* Include both controller and service layers if they contain logic, validation, or exception handling.
Output format (strictly use markdown):
### Functions to Test
1. **FunctionName(parameters)**
   - **Main Purpose:**  
     Brief one-line summary of what it does.
   - **Inputs:**  
     List each parameter (type + meaning).
   - **Returns:**  
     Data type and meaning of the return value.
   - **Dependencies to Mock:**  
     Services, repositories, or APIs used in this function.
   - **Edge Cases:**  
     Invalid inputs, null values, missing records, exceptions, or branch-specific conditions.
   - **Suggested Test Names:**  
     List of test names following the Given_When_Then convention.
Additional Notes:
*Focus on public and internal business-logic methods (e.g., controller endpoints, service logic like SubmitListeningAttempt, CalculateScore, SaveResult if they contain exception branches). 
*Identify mockable dependencies like IListeningRepository, IListeningAttemptRepository, IExamRepository, IUserRepository, etc. 
*Include authorization and validation logic in controllers, since these affect coverage (e.g., Unauthorized, BadRequest, NotFound, 500 branches). 
*Suggest test cases for both happy-path and failure branches (e.g., null DTO, invalid answers, missing exam attempt, database exception, double submission). 
*Keep each method entry concise but complete enough for automated test generation tools. 
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
