# External Services Test Prompts (Email, ImageConverter, Dictionary, SpeechToText, OpenAI)

## Phase 1 – Testing Specification

**Context:**  
IELTSPhobic External Services module (.NET 8, xUnit, Moq, FluentAssertions) — handles integration with external APIs and services including email, image conversion, OpenAI, dictionary APIs, and speech-to-text.  
Architecture: Service → External API Client.  

**Role:**  
Expert software test engineer and prompt engineer.  

**Goal:**  
Identify all **public/business-logic** methods that need **unit or integration testing** (>80% coverage).  

**Requirements:**  
- Ignore trivial getters/setters or mapping helpers.  
- Include Service methods with external API calls, data transformation, or exception handling.  
- List mockable dependencies (e.g., `HttpClient`, `IConfiguration`, `OpenAIClient`, `Cloudinary`, `MailKit`).  
- Mention edge cases (null inputs, invalid data, exceptions, API failures, network timeouts, rate limits).  
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
IELTSPhobic External Services (.NET 8 WebAPI + xUnit) — integrates with multiple external APIs and services.  

**Role:**  
Expert .NET test engineer.  

**Goal:**  
Generate a **Test Case Matrix** (>80% coverage) for all public methods.  

**Instructions:**  
1. Analyze classes (`EmailService`, `ImageConverter`, `DictionaryApiClient`, `SpeechToTextService`, `OpenAIService`).  
2. List **4–6 test cases** per method:  
   - Happy Path  
   - Edge Case  
   - Error  
   - Integration (mocked deps)  
3. Use **Given–When–Then** format.  
4. Show output as a Markdown table.  
5. Mention mocks (e.g., `HttpClient`, `OpenAIClient`, `IConfiguration`).  

**Output Format:**
| Category | Test Case | Input | Expected |
|-----------|------------|--------|-----------|
| Happy Path | Given valid email When sent | email data | Returns success |
| Edge Case | Given invalid image When converted | invalid format | Returns error |
| Error | Given API timeout | network error | Returns timeout exception |
| Integration | Given valid audio When transcribed | audio file | Returns transcribed text |

**Output Title:**  
`### AI Output: Test Cases Matrix for EmailService`

## Phase 3 – Code Generation

### Prompt 1 – EmailService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `EmailService` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `SmtpClient`, `MailMessage`, `IConfiguration`.
- Test all main public methods:
  - EmailService: `SendEmailAsync`, `SendVerificationEmail`, `SendPasswordResetEmail`, `SendNotificationEmail`
- Test email sending, template rendering, SMTP configuration.
- Include edge cases like invalid email addresses, SMTP failures, network timeouts.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 2 – ImageConverter
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `ImageConverter` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock image processing operations, test file I/O.
- Test all main public methods:
  - ImageConverter: `ConvertToBase64`, `ResizeImage`, `ValidateImageFormat`, `CompressImage`
- Test image conversion, format validation, resizing, compression.
- Include edge cases like invalid formats, oversized images, corrupted files.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 3 – OpenAI Service (Complete)
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `OpenAIService` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `OpenAIClient`, `IConfiguration`, `ILogger<OpenAIService>`.
- Test ALL public methods:
  - OpenAIService: `GradeWriting`, `SpeechToText`, `TranscribeAudio`, `GenerateResponse`
- Test AI grading, transcription, response generation.
- Mock OpenAI API responses (both success and failure cases).
- Test JSON parsing, error handling, rate limiting.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 4 – DictionaryApiClient
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `DictionaryApiClient` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `HttpClient`, `IConfiguration`.
- Test all main public methods:
  - DictionaryApiClient: `GetDefinition`, `SearchWord`, `GetSynonyms`, `GetAntonyms`, `GetExamples`
- Test dictionary API integration, response parsing, error handling.
- Mock HTTP responses for different status codes (200, 404, 500).
- Include edge cases like word not found, API rate limiting, network errors.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

### Prompt 5 – SpeechToTextService
You are an expert .NET test engineer.

Generate **fully compilable xUnit test code** (C#) for the `SpeechToTextService` class in the IELTSPhobic web application.
Do **not** create or save any file — only output the full C# source code as markdown.

**Requirements:**
- Namespace: `WebAPI.Tests`
- Use `xUnit`, `Moq`, and `FluentAssertions`.
- Mock `OpenAIClient`, `IConfiguration`, `ILogger<SpeechToTextService>`.
- Test all main public methods:
  - SpeechToTextService: `TranscribeAsync`, `TranscribeFromAudioFile`, `ValidateAudioFormat`, `GetTranscriptionStatus`
- Test audio transcription, format validation, error handling.
- Mock audio processing and OpenAI Whisper API responses.
- Include edge cases like unsupported formats, empty audio, corrupted files.
- Each `[Fact]` must be a standalone public test method.
- The output must be a single C# code block wrapped in ```csharp ... ``` with no extra commentary.

**Goal:**
Produce clean, syntactically correct test code that can compile under .NET 8.

## Phase 4 – Bug Fixes and Coverage

### Fix Failing Tests
You are an expert .NET test engineer.
I will give you a C# test class and the `dotnet test` output.
Analyze the failing tests and explain why each failed.
Determine whether the issue is in the test expectations or in the service logic.
Then show the exact corrected code snippet for each failed test.

**Format:**
1. Reason for failure
2. Corrected code snippet (test or service)

### Increase Coverage
I'm testing the External Services classes in an ASP.NET Core WebAPI project using xUnit and Moq.  
Currently, the test files pass all test cases, but the code coverage is only around 70%.  
Please analyze all logical branches and generate 5–10 additional test cases to raise the coverage above 80%.  

The new tests should:
- Cover untested branches, exceptions, skipped conditions, and defaults.
- Not duplicate existing test cases.
- Follow xUnit and Moq style (Fact + Arrange–Act–Assert).
- Mock all dependencies (no real external API calls).
- Output complete C# test methods that I can directly add to the test files.

### Mock External APIs
You are an expert .NET test engineer.

Help me create comprehensive test mocks for external services in the IELTSPhobic application:
- **OpenAI Service**: Mock ChatGPT API responses for essay grading, speech transcription
- **Email Service**: Mock SMTP client for email sending
- **Dictionary API**: Mock HTTP responses for word definitions
- **Cloudinary**: Mock image upload and manipulation

Generate helper methods and setup code for mocking these services using Moq and FakeHttpClient.
Each mock should handle both success and failure scenarios.
Output complete C# test helper classes that can be used across test files.

**Requirements:**
- Namespace: `WebAPI.Tests.Helpers`
- Use `Moq`, `FakeHttpClient`, and `Microsoft.AspNetCore.Hosting` if needed
- Include setup methods for each external service
- Handle common failure scenarios (timeouts, rate limits, invalid responses)
- The output must be a single C# code block with all helper classes


