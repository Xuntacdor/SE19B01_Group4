## 🧩 Phase 1 – Tetsting specification 

**Context:**  
IELTSPhobic AI Writing Grading module (.NET 8, xUnit, Moq, FluentAssertions) — integrates OpenAI for essay evaluation.  
Architecture: Controller → Service → External AI (OpenAI) → Repository.

**Role:**  
Expert software test engineer and prompt engineer.

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Controller + Service methods with validation, AI interaction, or exception handling.  
- List mockable dependencies (e.g., `IWritingRepository`, `IWritingFeedbackRepository`, `OpenAIService`, `IExamService`).  
- Mention edge cases (null inputs, invalid data, exceptions, unauthorized, 500 errors).  
- Suggest both **happy-path** and **failure** test cases.

**Output Format (Markdown):**
```markdown```
### Functions to Test
1. **FunctionName(params)**
   - **Main Purpose:** one-line summary  
   - **Inputs:** type + meaning  
   - **Returns:** type + purpose  
   - **Dependencies to Mock:**  
   - **Edge Cases:**  
   - **Suggested Test Names:** Given_When_Then...

## 🧩 Phase 2 – Test Case Matrix (AI Writing Grading)

**Context:**  
IELTSPhobic AI Writing Grading (.NET 8 WebAPI + xUnit) — uses OpenAI for essay scoring and feedback.

**Role:**  
Expert .NET test engineer.

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.

**Instructions:**  
1. Analyze one class (`WritingService`, `WritingController`, or `OpenAIService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `OpenAIService`, `IWritingRepository`).

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid essay When submitted | dto={examId:1, text:"Sample"} | Returns JSON feedback |
| Edge Case | Given null essay When submitted | dto=null | Returns BadRequest |
| Error | Given AI throws error | mock throws | Returns 500 |
| Integration | Given valid essay When graded | mocks active | Calls services once |

**Output Title:**  
`### AI Output: Test Cases Matrix for WritingService`


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
`Not enough code coverage fix`
I'm testing the WritingService class in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the WritingServiceTests.cs file passes all 14 test cases, but the code coverage is only around 75%.  
Please analyze all logical branches in WritingService (GetById, Delete, Update, GradeWriting, GradeSingle, GradeFull, and SaveFeedback) and generate 5–7 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real database or API calls).
- Output complete C# test methods that I can directly add to WritingServiceTests.cs.
