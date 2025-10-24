`Phase 1`
**Prompt 1:**

Context: This source code belongs to the IELTSPhobic web application — specifically, the AI Writing Grading module that interacts with OpenAI for essay evaluation. The system follows a multi-layer architecture: Controller → Service -> External AI (OpenAI)→ Repository 

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
* Focus on **public** and **internal business-logic** methods (e.g., controller endpoints, service logic like GradeWriting, SaveFeedback if it contains exception branches).
* Identify **mockable dependencies** like IWritingRepository, IWritingFeedbackRepository, OpenAIService, IExamService, etc.
* Include **authorization and validation logic** in controllers, since these affect coverage (e.g., Unauthorized, BadRequest, NotFound, 500 branches).
* Suggest test cases for **both happy-path and failure branches** (e.g., null DTO, invalid answers, exception in AI call, missing exam attempt).
* Keep each method entry concise but complete enough for automated test generation tools.

`Phase 2`
**Prompt 2:**


Analyze the following source code from my GitHub repository (IELTSPhobic project).
This code belongs to the **AI Writing Grading** feature built with **.NET 8 WebAPI + xUnit**.

**Goal:** Automatically generate a full **Test Case Matrix** table for every public method found in the class.

---

### Instructions:

1. Read and analyze the target class/function from the repository (e.g. `WritingService`, `WritingController`, or `OpenAIService`).
2. Identify all **testable functions** that contain logic (ignore trivial getters/setters).
3. For each function, create **4–6 comprehensive test cases** categorized into:

   * **Happy Path** — Normal behavior
   * **Edge Case** — Boundary conditions or nulls
   * **Error** — Exceptions or invalid inputs
   * **Integration** — Interaction with mocks/dependencies
4. Use the **Given–When–Then** format for clarity of scenario logic.
5. Present the results as a **Markdown table** with these columns:
   | Category | Test Case | Input | Expected |
6. Ensure at least **3–4 test cases per function** to achieve high coverage (>80% line & branch).
7. If the method interacts with other services or repositories, include notes for which dependencies will be mocked.


### Example Output Format:

```markdown
| Category   | Test Case                                    | Input                                  | Expected                          |
|-------------|----------------------------------------------|----------------------------------------|------------------------------------|
| Happy Path  | Add new item                                 | product={id:1, price:100}, qty=2       | items.length = 1                   |
| Happy Path  | Add existing item                            | same product, qty=3                    | quantity updated to 5              |
| Edge Case   | Quantity = 0                                 | product={...}, qty=0                   | throws Error                       |
| Edge Case   | Negative quantity                            | product={...}, qty=-5                  | throws Error                       |
| Error       | Null product                                 | product=null, qty=1                    | throws Error                       |
| Error       | Undefined product                            | product=undefined, qty=1               | throws Error                       |
```

---

**Deliverables:**

* Output must be in Markdown table format.
* Categorize clearly (Happy Path / Edge Case / Error / Integration).
* The goal is to design test cases **before** writing actual xUnit code.
* Keep it concise but sufficient to ensure full branch coverage.

**Output title:**
`### AI Output: Test Cases Matrix for <ClassName>`
`Phase 3 :`
`Prompt 1 – WritingService`
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `WritingService` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

Requirements:
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock dependencies: `IWritingRepository`, `IWritingFeedbackRepository`, `OpenAIService`, and `IExamService`.
- Test all main public methods: `GetById`, `GetByExam`, `Create`, `Update`, `Delete`, `GradeWriting`.
- Include helper methods like `CreateSampleFeedback()` and `SetupSaveFeedback()`.
- Use realistic data and assertions that fully compile.
- Ensure JSON strings are properly escaped (no `\"` issues).
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

Goal:
Produce clean, syntactically correct test code that can compile under .NET 8.


`Prompt 2 – WritingController`
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `WritingController` class in the IELTSPhobic web application.  
Do **not** create or save any file — only output the full C# source code as markdown.

Requirements:
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock dependencies: `IWritingService` (the controller uses this service).
- Test all main public methods: `GetById`, `GetByExam`, `Create`, `Update`, `Delete`, `GradeWriting`.
- Include both success and failure (BadRequest, NotFound, Exception) scenarios.
- Use realistic data and assertions that fully compile.
- Ensure JSON and DTOs are properly structured (no escaping issues).
- Each `[Fact]` must be a standalone public test method.
- Output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

Goal:  
Produce clean, syntactically correct test code that can compile under .NET 8.

`Prompt 3 – OpenAIService`
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `OpenAIService` class in the IELTSPhobic web application.  
Do **not** create or save any file — only output the full C# source code as markdown.

Scope:  
Focus **only** on the **Writing Grading** feature (`GradeWriting` method) — ignore `SpeechToText` and `GradeSpeaking`.

Requirements:
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock dependencies: `ILogger<OpenAIService>` and `IConfiguration`.
- Use a **fake or in-memory `OpenAIClient`** by mocking its internal `GetChatClient` and `CompleteChat` behavior through wrappers or delegates.
- Test scenarios:
  1. **Happy Path**: Valid question and answer → returns valid JSON.
  2. **Image Provided**: Valid Base64 conversion simulated.
  3. **Image Conversion Fails**: Exception thrown inside image conversion, logs warning, still returns valid JSON.
  4. **OpenAI Throws Exception**: Mock exception → JSON containing `"error"` property.
  5. **Invalid JSON Returned**: Chat response contains malformed text → fallback to empty `{}`.
- Ensure **all JSON strings** are valid and escaped correctly.
- Each `[Fact]` must be a standalone, public test method using `Arrange–Act–Assert`.
- Include any helper method like `CreateFakeResponseJson()` for reusable JSON payloads.
- Output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

Goal:  
Produce clean, syntactically correct, fully self-contained unit test code for `GradeWriting()` that compiles under .NET 8.
`Refractor the wrong test case`
Help me fix this failing xUnit test in my .NET project.

**ERROR:**
Expected result.Value to be WebAPI.DTOs.WritingDTO, but found <null>.

**TEST CODE (from WritingControllerTests.cs):**
`csharp`
[Fact]
public void GivenValidId_WhenGetById_ThenReturnsOk()
{
    var dto = new WritingDTO
    {
        WritingId = 1,
        ExamId = 2,
        WritingQuestion = "Q",
        DisplayOrder = 1,
        CreatedAt = DateTime.UtcNow,
        ImageUrl = "img"
    };

    _writingServiceMock.Setup(s => s.GetById(1)).Returns(dto);

    var controller = CreateController();
    var result = controller.GetById(1);

    result.Should().BeOfType<OkObjectResult>();
    result.Value.Should().Be(dto);
}
SOURCE CODE (from WritingController.cs):
[HttpGet("{id}")]
[AllowAnonymous]
public ActionResult<WritingDTO> GetById(int id)
{
    var result = _writingService.GetById(id);
    return result == null ? NotFound() : Ok(result);
}
Task: Explain clearly:

Why the test failed (root cause).

Whether the issue comes from the controller logic or test setup (mocking).

Show corrected code for the test and/or controller so the test passes.

Keep the fix realistic and consistent with dependency injection and DTO mapping patterns.
`OpenAIServices test fix bug`