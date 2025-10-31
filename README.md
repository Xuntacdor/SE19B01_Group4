[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE) [![.NET](https://img.shields.io/badge/backend-.NET%208.0-blue)](https://dotnet.microsoft.com/) [![React](https://img.shields.io/badge/frontend-React%2018-blue)](https://reactjs.org/) [![Issues](https://img.shields.io/github/issues/dsaPhobic/IELTSPracticeSystem)](https://github.com/dsaPhobic/IELTSPracticeSystem/issues) [![Stars](https://img.shields.io/github/stars/dsaPhobic/IELTSPracticeSystem)](https://github.com/dsaPhobic/IELTSPracticeSystem) [![Forks](https://img.shields.io/github/forks/dsaPhobic/IELTSPracticeSystem)](https://github.com/dsaPhobic/IELTSPracticeSystem/network/members)
# IELTS Learning Platform

A comprehensive web application designed for IELTS test preparation, featuring interactive exam modules, AI-powered feedback, and a community forum for learners.

## 🚀 Features

### Core Learning Modules
- **📖 Reading Tests**: Interactive reading comprehension with multiple choice questions
- **🎧 Listening Tests**: Audio-based listening exercises with various question types
- **✍️ Writing Tests**: Essay writing with AI-powered feedback and scoring
- **🗣️ Speaking Tests**: Voice recording with speech-to-text and AI analysis
- **📚 Dictionary**: Word search, definitions, and personal vocabulary management

### Community Features
- **💬 Forum System**: Post creation, commenting, and discussion threads
- **🏷️ Tag System**: Content categorization and filtering
- **👍 Voting System**: Post and comment likes/votes
- **📎 File Attachments**: Support for images and documents in posts
- **🚨 Reporting System**: Content moderation and user reporting

### User Management
- **👤 User Authentication**: Registration, login, and password reset
- **🔐 Google OAuth**: Social login integration
- **👑 VIP Subscription**: Premium features with Stripe payment integration
- **👥 Role-Based Access**: User, Moderator, and Admin roles

### Admin & Moderation
- **⚙️ Admin Dashboard**: Exam management and user administration
- **🛡️ Moderation Tools**: Content approval, tag management, and report handling
- **📊 Analytics**: Transaction monitoring and user statistics

## 🛠️ Technology Stack

### Frontend
- **React 18** with modern hooks and functional components
- **Vite** for fast development and building
- **React Router** for client-side routing
- **Axios** for HTTP requests
- **CSS Modules** for component styling
- **Lucide React** for icons

### Backend
- **ASP.NET Core 8.0** Web API
- **Entity Framework Core** with SQL Server
- **Cookie Authentication** with Google OAuth
- **Swagger/OpenAPI** for API documentation

### External Services
- **OpenAI API** for AI-powered writing and speaking feedback
- **Cloudinary** for file storage and image processing
- **Stripe** for payment processing
- **Email Service** for notifications and OTP verification
- **Dictionary API** for word definitions

### Database
- **SQL Server** with comprehensive schema
- **Entity Framework Core** migrations
- **Optimized queries** with proper indexing

## 📁 Project Structure

```
SE19B01_Group4/
├── react-app/                 # Frontend React application
│   ├── src/
│   │   ├── Components/        # Reusable UI components
│   │   │   ├── Admin/         # Admin-specific components
│   │   │   ├── Auth/          # Authentication components
│   │   │   ├── Dictionary/    # Dictionary components
│   │   │   ├── Exam/          # Exam-related components
│   │   │   ├── Forum/         # Forum components
│   │   │   ├── Layout/        # Layout components
│   │   │   ├── Moderator/     # Moderator components
│   │   │   └── UI/            # General UI components
│   │   ├── Pages/             # Page components
│   │   │   ├── Admin/         # Admin pages
│   │   │   ├── Authenciation/ # Auth pages
│   │   │   ├── Dashboard/     # Dashboard pages
│   │   │   ├── Dictionary/    # Dictionary pages
│   │   │   ├── Forum/         # Forum pages
│   │   │   ├── Listening/     # Listening test pages
│   │   │   ├── Moderator/     # Moderator pages
│   │   │   ├── Profile/       # User profile pages
│   │   │   ├── Reading/       # Reading test pages
│   │   │   ├── Speaking/      # Speaking test pages
│   │   │   ├── Transactions/ # Payment pages
│   │   │   └── Writing/       # Writing test pages
│   │   ├── Services/          # API service modules
│   │   ├── Hook/              # Custom React hooks
│   │   └── utils/             # Utility functions
│   └── package.json
├── WebAPI/                    # Backend API
│   └── WebAPI/
│       ├── Controllers/       # API controllers
│       ├── Models/           # Database models
│       ├── Services/         # Business logic services
│       ├── Repositories/     # Data access layer
│       ├── DTOs/             # Data transfer objects
│       ├── ExternalServices/ # External API integrations
│       └── Data/             # Database context
└── *.puml                    # UML diagrams
```

## 🚀 Getting Started

### Prerequisites
- **Node.js** (v16 or higher)
- **.NET 8 SDK**
- **SQL Server** (LocalDB or full instance)
- **Visual Studio** or **VS Code**

### Installation

#### Backend Setup
1. Navigate to the WebAPI directory:
   ```bash
   cd WebAPI/WebAPI
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Update connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Your SQL Server connection string"
     }
   }
   ```

4. Run database migrations:
   ```bash
   dotnet ef database update
   ```

5. Start the API:
   ```bash
   dotnet run
   ```

#### Frontend Setup
1. Navigate to the react-app directory:
   ```bash
   cd react-app
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```
   - Dev server chạy tại `http://localhost:5173` và proxy `/api` tới backend `https://localhost:7264` (xem `react-app/vite.config.js`).

### Environment Configuration

#### Backend Configuration
Update `appsettings.json` with your external service credentials:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  },
  "Cloudinary": {
    "CloudName": "your-cloudinary-cloud-name",
    "ApiKey": "your-cloudinary-api-key",
    "ApiSecret": "your-cloudinary-api-secret"
  },
  "Stripe": {
    "SecretKey": "your-stripe-secret-key"
  }
}
```

## 📖 API Documentation

The API documentation is available via Swagger UI when running the backend:
- **Development**: `https://localhost:7264/swagger`
- **Production**: `https://your-domain.com/swagger`

### Key API Endpoints

#### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `GET /api/auth/me` - Get current user info

#### Forum
- `GET /api/forum/posts` - Get posts with pagination
- `POST /api/forum/posts` - Create new post
- `GET /api/forum/posts/{id}` - Get specific post
- `PUT /api/forum/posts/{id}` - Update post
- `DELETE /api/forum/posts/{id}` - Delete post

#### Exams
- `GET /api/exam` - Get available exams
- `POST /api/exam/attempt` - Submit exam attempt
- `GET /api/exam/attempt/{id}` - Get exam results

#### Skills
- `Reading/Listening/Speaking/Writing` endpoints for practice and feedback

#### Media & Upload
- `POST /api/upload` - Upload files (Cloudinary)

#### Tags & Dictionary
- `GET /api/tags` - Tag management
- `GET /api/words` / `GET /api/vocab-groups`

#### Notifications
- `GET /api/notifications` - User notifications

#### VIP & Payments
- `GET /api/vip-plans` - List VIP plans
- `POST /api/vip-payments` - Create payment (Stripe)
- `POST /api/stripe/webhook` - Stripe webhook

#### Transactions
- `GET /api/transactions` - Transaction history

#### Admin & Moderator
- `GET /api/admin/...` - Admin operations
- `GET /api/moderator/...` - Moderation operations

## 🎯 User Roles & Permissions

### Regular User
- Take practice exams
- Access dictionary and vocabulary tools
- Create and participate in forum discussions
- Purchase VIP subscriptions

### VIP User
- All regular user features
- Access to premium exam content
- Priority support
- Advanced analytics

### Moderator
- All user features
- Approve/reject forum posts
- Manage tags and categories
- Handle user reports

### Admin
- All moderator features
- Create and manage exams
- User management
- Transaction monitoring
- System configuration

## 🔧 Development

### Code Style
- **Frontend**: ESLint configuration for React
- **Backend**: C# coding conventions
- **Database**: Snake_case naming convention

### Testing
- **Backend**: Unit tests with xUnit and Moq

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## 🚀 Deployment

### Backend Deployment
1. Build the project:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Deploy to your hosting provider (Azure, AWS, etc.)

### Frontend Deployment
1. Build the React app:
   ```bash
   npm run build
   ```

2. Deploy the `dist` folder to your static hosting provider

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Team

**SE19B01_Group4** - FPT University Software Engineering Project

## 📞 Support

For support and questions, please contact the development team or create an issue in the repository.

---

**Built with ❤️ for IELTS learners worldwide**
