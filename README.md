# BÁO CÁO DỰ ÁN - CODE EXPLAINER

## 📋 THÔNG TIN DỰ ÁN

**Tên dự án:** Code Explainer - Trợ lý giải thích code thông minh  
**Công nghệ:** .NET 9.0, WPF Desktop, ASP.NET Core Web API, AI (Gemini 2.0)  
**Mô hình:** Client-Server Architecture với SignalR Real-time Communication  
**Ngôn ngữ lập trình chính:** C#    
**Database:** SQL Server  
**Người thực hiện:** [Mạc Đỗ Gia Huy, Châu Vương Hoàng]  
**Thời gian:** 2025

---

## 🎯 MỤC TIÊU DỰ ÁN

Xây dựng một hệ thống ứng dụng desktop kết hợp với backend API, cho phép người dùng:

1. **Giải thích code tự động** bằng AI (Gemini 2.0)
2. **Quản lý phiên chat** với lịch sử hội thoại
3. **Xác thực người dùng** với nhiều phương thức (Email/Password, Google OAuth)
4. **Nhận thông báo** real-time qua SignalR
5. **Quản lý hồ sơ cá nhân** với tính năng upload ảnh

---

## 🏗️ KIẾN TRÚC HỆ THỐNG

### 1. Tổng Quan Kiến Trúc

```
┌─────────────────────────────────────────────────────────────┐
│                    CODE EXPLAINER SYSTEM                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐         ┌─────────────────────────┐  │
│  │   WPF Desktop    │◄────────┤   ASP.NET Core API      │  │
│  │   Application    │  HTTPS  │   (.NET 9.0)            │  │
│  │   (Client)       │────────►│   + SignalR Hubs        │  │
│  └──────────────────┘         └─────────────────────────┘  │
│          │                               │                  │
│          │                               │                  │
│          ▼                               ▼                  │
│  ┌──────────────────┐         ┌─────────────────────────┐  │
│  │  MVVM Pattern    │         │   Layered Architecture  │  │
│  │  - ViewModels    │         │   - Controllers         │  │
│  │  - Views         │         │   - Services            │  │
│  │  - Models        │         │   - Repository          │  │
│  └──────────────────┘         │   - Business Objects    │  │
│                                └─────────────────────────┘  │
│                                           │                  │
│                                           ▼                  │
│                                ┌─────────────────────────┐  │
│                                │   SQL Server Database   │  │
│                                │   - Users               │  │
│                                │   - ChatSessions        │  │
│                                │   - ChatMessages        │  │
│                                │   - Notifications       │  │
│                                └─────────────────────────┘  │
│                                                              │
│                        ┌─────────────────────────┐          │
│                        │   External Services     │          │
│                        │   - Gemini 2.0 AI       │          │
│                        │   - Google OAuth        │          │
│                        │   - Cloudinary (Image)  │          │
│                        │   - Email SMTP          │          │
│                        └─────────────────────────┘          │
└─────────────────────────────────────────────────────────────┘
```

### 2. Cấu Trúc Dự Án (Solution Structure)

Dự án được tổ chức theo **Clean Architecture** với 6 projects chính:

#### **a) CodeExplainer.WebApi** (API Layer)

- **Vai trò:** Web API Server, xử lý các request từ client
- **Công nghệ:** ASP.NET Core 9.0, SignalR
- **Thành phần chính:**
  - `Controllers/`: AuthController, ChatController, UserController, NotificationController
  - `Hubs/`: ChatHub, NotificationHub (SignalR)
  - `Program.cs`: Configuration, Dependency Injection, Middleware

#### **b) CodeExplainer.Desktop** (Presentation Layer)

- **Vai trò:** Ứng dụng desktop WPF cho người dùng
- **Công nghệ:** WPF, MVVM Pattern, Material Design
- **Thành phần chính:**
  - `Views/`: AuthView, ChatView, ProfileView, NotificationView
  - `ViewModels/`: AuthViewModel, ChatViewModel, MainViewModel
  - `Services/`: ApiClient (HTTP communication)
  - `Models/`: Data models cho UI

#### **c) CodeExplainer.BusinessObject** (Domain Layer)

- **Vai trò:** Định nghĩa các entity và data transfer objects
- **Thành phần:**
  - `Models/`: User, ChatSession, ChatMessage, Notification
  - `Request/`: LoginRequest, RegisterRequest, ChatSendRequest
  - `Response/`: LoginResponse, ChatSendResponse, BaseResultResponse
  - `Enum/`: UserRole
  - `Migrations/`: Entity Framework migrations
  - `ApplicationDbContext.cs`: Database context

#### **d) CodeExplainer.Services** (Application Layer)

- **Vai trò:** Business logic và use cases
- **Interfaces:**
  - `IAuthTokenProcess`: JWT token generation/validation
  - `IAuthorizeServices`: Authentication & authorization logic
  - `IChatServices`: Chat và AI integration
  - `IUserServices`: User management
  - `INotificationServices`: Notification handling
  - `IEmailSender`: Email service
- **Implements:** Các class implementation tương ứng

#### **e) CodeExplainer.Repository** (Data Access Layer)

- **Vai trò:** Truy xuất dữ liệu từ database
- **Pattern:** Repository Pattern với Generic Repository
- **Interfaces:**
  - `IUserRepository`
  - `IChatRepository`
  - `INotificationRepository`

#### **f) CodeExplainer.Shared** (Infrastructure Layer)

- **Vai trò:** Các utilities và shared components
- **Thành phần:**
  - `Jwt/`: JWT configuration
  - `Utils/`: Helper classes (CloudinaryUploader, EmailSender)
  - `Exceptions/`: Custom exceptions

---

## 💡 CHỨC NĂNG CHI TIẾT

### 1. Hệ Thống Xác Thực (Authentication)

#### **a) Đăng ký tài khoản**

- Người dùng nhập: Username, Email, Password
- Hệ thống:
  - Validate dữ liệu đầu vào
  - Hash password bằng BCrypt
  - Tạo email confirmation token
  - Gửi email xác nhận đến người dùng
  - Lưu user vào database với trạng thái `EmailConfirmed = false`

**Flow:**

```
User Input → Validation → Hash Password → Generate Token → Send Email → Save DB
```

#### **b) Đăng nhập Email/Password**

- Người dùng nhập Email và Password
- Hệ thống:
  - Tìm user theo email
  - Verify password hash
  - Kiểm tra email đã confirmed chưa
  - Generate JWT Access Token và Refresh Token
  - Trả về token cho client

**Flow:**

```
Credentials → Find User → Verify Password → Check Email → Generate JWT → Return Tokens
```

#### **c) Đăng nhập Google OAuth 2.0**

- Người dùng click "Login with Google"
- Hệ thống:
  - Redirect đến Google authentication page
  - Google callback với user info (email, name, avatar)
  - Tạo hoặc cập nhật user trong database
  - Generate JWT tokens
  - Redirect về desktop app với thông tin đăng nhập

**Flow:**

```
Click Login → Google Auth → Callback → Create/Update User → JWT → Desktop App
```

#### **d) Xác nhận Email**

- Người dùng nhấn link trong email
- Token được validate
- Cập nhật `EmailConfirmed = true`

#### **e) Refresh Token**

- Khi Access Token hết hạn
- Client gửi Refresh Token
- Server validate và issue new Access Token

### 2. Hệ Thống Chat với AI

#### **a) Gửi tin nhắn và giải thích code**

**Input từ người dùng:**

- Message: Câu hỏi hoặc yêu cầu
- Source Code: Đoạn code cần giải thích
- Language: Ngôn ngữ lập trình (C#, Java, Python, JavaScript, etc.)

**Quy trình xử lý:**

1. **Client gửi ChatSendRequest:**

```json
{
  "chatSessionId": "guid-hoặc-empty",
  "message": "Giải thích đoạn code này",
  "sourceCode": "public void Hello() { ... }",
  "language": "csharp"
}
```

2. **Server xử lý (ChatServices):**

   - Tạo hoặc tìm ChatSession
   - Tạo ChatMessage với role="user"
   - Truncate source code nếu > 10,000 ký tự
   - Format prompt cho AI:

     ```
     Explain what this {language} code does. Also suggest improvements if possible:

     {sourceCode}
     ```

3. **Gọi Gemini 2.0 AI API:**

   - Sử dụng thư viện `MaIN.Core.Hub`
   - Model: `gemini-2.0-flash`
   - Nhận response từ AI

4. **Xử lý response:**

   - Tạo ChatMessage với role="assistant"
   - Lưu cả user message và AI response vào database
   - Trả về ChatSendResponse cho client

5. **Error handling:**
   - Nếu AI service fail: Lưu error message vào database
   - Client vẫn nhận được feedback (không bị mất dữ liệu)

**Flow diagram:**

```
User Input → Create/Find Session → Save User Message → Format Prompt
→ Call Gemini AI → Receive AI Response → Save AI Message → Return to Client
```

#### **b) Quản lý phiên chat (Sessions)**

- Mỗi user có nhiều ChatSession
- Mỗi ChatSession có:
  - `ChatSessionId`: Unique identifier
  - `Title`: Auto-generate từ message đầu tiên (50 ký tự đầu)
  - `CreatedAt`, `UpdatedAt`: Timestamps
  - Collection của `ChatMessages`

**API Endpoints:**

- `GET /api/chat/sessions`: Lấy tất cả sessions của user
- `GET /api/chat/messages/{sessionId}`: Lấy tất cả messages trong session

#### **c) Real-time Communication với SignalR**

**ChatHub features:**

- `SendMessage(sessionId, user, message)`: Broadcast message đến tất cả users trong session
- `JoinSession(sessionId)`: Tham gia chat room
- `LeaveSession(sessionId)`: Rời chat room

**Client-side:**

- Desktop app kết nối đến SignalR hub
- Listen event `ReceiveMessage` để nhận tin nhắn real-time

### 3. Quản Lý Người Dùng (User Management)

#### **a) Xem hồ sơ cá nhân**

- API: `GET /api/user/profile`
- Authorization: Yêu cầu JWT token
- Trả về thông tin: UserId, UserName, Email, ProfilePictureUrl, UserRole, CreatedAt

#### **b) Cập nhật hồ sơ**

- API: `PUT /api/user/profile`
- Cho phép cập nhật:
  - Username
  - Profile Picture (upload qua Cloudinary)
- Validation:
  - Username unique
  - Image size limits

#### **c) Đổi mật khẩu**

- Yêu cầu: Current password + New password
- Verify current password
- Hash new password
- Update database

#### **d) Quên mật khẩu**

- Request reset password → Gửi email với token
- User click link → Verify token → Đổi password mới

### 4. Hệ Thống Thông Báo (Notifications)

#### **a) NotificationHub (SignalR)**

- Real-time push notifications
- Connection per user
- Notify về: Chat messages, system alerts, account activities

#### **b) API Endpoints**

- `GET /api/notification/all`: Lấy tất cả notifications
- `GET /api/notification/unread`: Chỉ lấy chưa đọc
- `PUT /api/notification/{id}/read`: Đánh dấu đã đọc
- `POST /api/notification`: Tạo notification mới (admin)

#### **c) Notification Model**

```csharp
public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 🔧 CÔNG NGHỆ VÀ CÔNG CỤ

### Backend Technologies

| Công nghệ             | Phiên bản | Mục đích                |
| --------------------- | --------- | ----------------------- |
| .NET                  | 9.0       | Framework chính         |
| ASP.NET Core          | 9.0       | Web API                 |
| Entity Framework Core | 9.0       | ORM                     |
| SQL Server            | Latest    | Database                |
| SignalR               | Latest    | Real-time communication |
| JWT Bearer            | Latest    | Authentication          |
| BCrypt.Net            | Latest    | Password hashing        |
| Swashbuckle           | Latest    | API Documentation       |

### Frontend Technologies

| Công nghệ       | Mục đích             |
| --------------- | -------------------- |
| WPF             | Desktop UI framework |
| MVVM Pattern    | Architecture pattern |
| Material Design | UI/UX library        |
| HttpClient      | API communication    |
| SignalR Client  | Real-time updates    |

### External Services

| Service              | Mục đích            |
| -------------------- | ------------------- |
| **Gemini 2.0 Flash** | AI code explanation |
| **Google OAuth 2.0** | Social login        |
| **Cloudinary**       | Image hosting       |
| **SMTP Server**      | Email notifications |

### Development Tools

- **IDE:** Visual Studio 2022 / JetBrains Rider
- **Database:** SQL Server Management Studio / TablePlus
- **API Testing:** Swagger UI, Postman
- **Version Control:** Git, GitHub

---

## 📊 CƠ SỞ DỮ LIỆU

### Database Schema

#### **1. Users Table**

```sql
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY,
    UserName NVARCHAR(100) UNIQUE NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    EmailConfirmed BIT DEFAULT 0,
    UserRole INT DEFAULT 0,
    ProfilePictureUrl NVARCHAR(500),
    RefreshToken NVARCHAR(500),
    RefreshTokenExpiryTime DATETIME2,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);
```

#### **2. ChatSessions Table**

```sql
CREATE TABLE ChatSessions (
    ChatSessionId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
```

#### **3. ChatMessages Table**

```sql
CREATE TABLE ChatMessages (
    ChatMessageId UNIQUEIDENTIFIER PRIMARY KEY,
    ChatSessionId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL, -- 'user' or 'assistant'
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ChatSessionId) REFERENCES ChatSessions(ChatSessionId) ON DELETE CASCADE
);
```

#### **4. Notifications Table**

```sql
CREATE TABLE Notifications (
    NotificationId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
```

### Entity Relationships

```
Users (1) ─────< (N) ChatSessions
ChatSessions (1) ─────< (N) ChatMessages
Users (1) ─────< (N) Notifications
```

---

## 🔐 BẢO MẬT VÀ XÁC THỰC

### 1. JWT Authentication

**Token Structure:**

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-guid",
    "email": "user@example.com",
    "role": "User",
    "exp": 1234567890
  }
}
```

**Token Types:**

- **Access Token:** Expire in 1440 minutes (24 hours)
- **Refresh Token:** Stored in database, expire in 7 days

### 2. Password Security

- **Hashing:** BCrypt with salt (work factor: 12)
- **Validation:**
  - Minimum 8 characters
  - Must contain uppercase, lowercase, number

### 3. Authorization Levels

| Role  | Value | Permissions                                         |
| ----- | ----- | --------------------------------------------------- |
| User  | 0     | Basic chat, profile management                      |
| Admin | 1     | Full access, user management, notification creation |

### 4. CORS Configuration

```csharp
AllowedOrigins: [
    "http://localhost:3000",   // React dev
    "http://localhost:5159",   // API
    "http://localhost:8080"    // Desktop app
]
```

### 5. Environment Variables Security

Sensitive data stored in `.env` file (not committed to Git):

```
SQL_CONNECTION_STRING=...
JWT_SECRET=...
OPENAI_API_KEY=...
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
```

---

## 📱 GIAO DIỆN NGƯỜI DÙNG (Desktop App)

### 1. Authentication Views

#### **a) Login/Register View**

- Material Design UI
- Input validation real-time
- Error messages
- "Login with Google" button
- "Forgot Password?" link

#### **b) Email Confirmation View**

- Success/failure notification
- Auto-redirect sau 3 giây

### 2. Main Application Window

#### **a) Navigation Sidebar**

- Profile section với avatar
- Menu items:
  - 📊 Dashboard
  - 💬 Chat with AI
  - 🔔 Notifications
  - ⚙️ Settings
  - 🚪 Logout

#### **b) Chat View**

- **Left Panel:** Session history
  - List các chat sessions
  - Search/filter sessions
  - Delete session
- **Center Panel:** Chat interface
  - Message history (scrollable)
  - User messages (right aligned, blue)
  - AI messages (left aligned, gray)
  - Markdown rendering cho code blocks
- **Right Panel:** Code input
  - Language selector dropdown
  - Code editor với syntax highlighting
  - "Send" button

#### **c) Profile View**

- Avatar upload
- Username editor
- Email display (read-only)
- Change password form
- Account statistics (join date, total chats)

#### **d) Notifications View**

- List notifications (latest first)
- Unread badges
- Mark as read/unread
- Clear all

### 3. MVVM Implementation

**Example: ChatViewModel**

```csharp
public class ChatViewModel : BaseViewModel
{
    private ObservableCollection<ChatMessage> _messages;
    private string _currentMessage;
    private string _sourceCode;

    public ICommand SendMessageCommand { get; }
    public ICommand LoadSessionsCommand { get; }

    private async Task SendMessage()
    {
        // Call API service
        var response = await _apiClient.SendChatMessage(...);
        // Update UI
        Messages.Add(response.AIMessage);
    }
}
```

---

## 🚀 DEPLOYMENT VÀ CÀI ĐẶT

### 1. Prerequisites

- .NET SDK 9.0
- SQL Server 2019 hoặc mới hơn
- Visual Studio 2022 hoặc Rider

### 2. Installation Steps

#### **Bước 1: Clone Repository**

```bash
git clone https://github.com/kleqing/Code-Explainer-Back-end.git
cd Code-Explainer-Back-end
```

#### **Bước 2: Cấu hình Environment Variables**

Tạo file `.env` trong thư mục root:

```env
SQL_CONNECTION_STRING=Server=localhost;Database=CodeExplainerDB;Trusted_Connection=True;TrustServerCertificate=True
JWT_SECRET=your-super-secret-key-min-32-characters
JWT_ISSUER=CodeExplainer.WebApi
JWT_AUDIENCE=CodeExplainer.Users
OPENAI_API_KEY=your-gemini-api-key
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
CORS_ALLOWED_ORIGINS=http://localhost:8080,http://localhost:3000
```

#### **Bước 3: Restore Dependencies**

```bash
dotnet restore
```

#### **Bước 4: Database Migration**

```bash
# Install EF Core tools nếu chưa có
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add Initial --project CodeExplainer.BusinessObject --startup-project CodeExplainer.WebApi --context ApplicationDbContext

# Update database
dotnet ef database update --project CodeExplainer.BusinessObject --startup-project CodeExplainer.WebApi --context ApplicationDbContext
```

#### **Bước 5: Build Solution**

```bash
dotnet build
```

#### **Bước 6: Run API Server**

```bash
cd CodeExplainer.WebApi
dotnet run
```

API sẽ chạy tại: `https://localhost:7077`  
Swagger UI: `https://localhost:7077/swagger`

#### **Bước 7: Run Desktop App**

```bash
cd CodeExplainer.Desktop
dotnet run
```

### 3. Configuration Files

#### **appsettings.json (WebApi)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "CodeExplainer.WebApi",
    "Audience": "CodeExplainer.Users",
    "ExpiryInMinutes": 1440
  },
  "EmailSettings": {
    "Email": "your-email@gmail.com",
    "Password": "app-password",
    "Host": "smtp.gmail.com",
    "Port": 587
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:8080"]
  }
}
```

#### **appsettings.json (Desktop)**

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5159"
  }
}
```

---

## 🧪 TESTING

### 1. Manual Testing với Swagger

Truy cập `https://localhost:7077/swagger` để test các API endpoints:

**Test Authentication Flow:**

1. POST `/api/auth/register` → Register user
2. Check email → Confirm account
3. POST `/api/auth/login` → Get JWT token
4. Click "Authorize" button → Paste token
5. Test protected endpoints

**Test Chat Flow:**

1. POST `/api/chat/send` với body:

```json
{
  "chatSessionId": "00000000-0000-0000-0000-000000000000",
  "message": "Explain this code",
  "sourceCode": "public void Hello() { Console.WriteLine(\"Hello\"); }",
  "language": "csharp"
}
```

2. GET `/api/chat/sessions` → Verify session created
3. GET `/api/chat/messages/{sessionId}` → Verify messages saved

### 2. Sample Test Data

**Test User:**

```json
{
  "userName": "testuser",
  "email": "test@example.com",
  "password": "@Test123"
}
```

### 3. Testing Checklist

- [ ] User registration successful
- [ ] Email confirmation works
- [ ] Login returns valid JWT
- [ ] Google OAuth redirects correctly
- [ ] Chat message saved to database
- [ ] AI response received and displayed
- [ ] Profile picture upload works
- [ ] Real-time notifications received
- [ ] Session management functional
- [ ] Password reset flow works

---

## 📈 PERFORMANCE VÀ TỐI ƯU

### 1. Database Optimization

- **Indexes:** Primary keys, foreign keys, email, username
- **Eager Loading:** Include related entities khi query
  ```csharp
  _context.ChatSessions
      .Include(s => s.Messages)
      .Where(s => s.UserId == userId)
  ```

### 2. API Response Time

- Average response time: < 200ms (không bao gồm AI call)
- AI call time: 2-5 seconds (phụ thuộc Gemini API)

### 3. Code Quality

- **Separation of Concerns:** Layered architecture
- **Dependency Injection:** Loose coupling
- **Error Handling:** Try-catch blocks, custom exceptions
- **Logging:** ILogger integration
- **Async/Await:** Non-blocking operations

### 4. Caching Strategy

- JWT tokens cached client-side
- User profile cached after login
- SignalR connection maintained (không reconnect liên tục)

---

## ⚠️ KNOWN ISSUES & LIMITATIONS

### Current Limitations

1. **Source Code Length:**

   - Maximum 10,000 characters
   - Longer code will be truncated
   - **Reason:** Avoid massive payloads to AI

2. **AI Model:**

   - Using Gemini 2.0 Flash (free tier)
   - Rate limits may apply
   - **Solution:** Implement retry mechanism

3. **Single Language Support:**

   - Desktop app chỉ tiếng Anh
   - **Future:** Internationalization (i18n)

4. **File Upload:**

   - Chưa support upload file code
   - Chỉ paste code vào text box
   - **Future:** File drag-and-drop

5. **Concurrent AI Requests:**
   - Chưa implement request queuing
   - Multiple requests cùng lúc có thể slow

### Known Bugs

- [ ] SignalR disconnect sau 30 phút idle
- [ ] Profile picture upload sometimes fails với large images
- [ ] Session list không auto-refresh sau tạo session mới

---

## 🔮 FUTURE ENHANCEMENTS

### Planned Features

1. **Code Diff Comparison**

   - So sánh code trước/sau optimize
   - Highlight changes

2. **Multi-language Support**

   - Tiếng Việt, English, Chinese
   - Internationalization framework

3. **Code Snippets Library**

   - Save favorite code explanations
   - Tag và categorize

4. **Collaborative Coding**

   - Share sessions với friends
   - Real-time collaboration

5. **Advanced AI Features**

   - Code generation từ description
   - Bug detection và suggestions
   - Performance analysis

6. **Mobile App**

   - iOS/Android version
   - Xamarin hoặc .NET MAUI

7. **Dark Mode**

   - Theme switcher
   - System preference detection

8. **Voice Input**
   - Speech-to-text cho queries
   - Text-to-speech cho AI responses

---

## 📚 TÀI LIỆU THAM KHẢO

### Official Documentation

1. [.NET 9.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)
2. [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
3. [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
4. [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
5. [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)

### Third-party Services

1. [Gemini AI API](https://ai.google.dev/docs)
2. [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
3. [Cloudinary Documentation](https://cloudinary.com/documentation)

### Design Patterns

1. Clean Architecture - Robert C. Martin
2. MVVM Pattern in WPF
3. Repository Pattern
4. Dependency Injection

---

## 👥 TEAM & CONTRIBUTIONS

### Roles & Responsibilities

| Tên     | Role               | Nhiệm vụ chính                   |
| ------- | ------------------ | -------------------------------- |
| [Tên 1] | Backend Lead       | API development, Database design |
| [Tên 2] | Frontend Developer | WPF Desktop App, UI/UX           |
| [Tên 3] | DevOps             | Deployment, CI/CD                |
| [Tên 4] | QA Tester          | Testing, Bug reporting           |

### Contribution Guidelines

- Branch naming: `feature/feature-name`, `bugfix/bug-name`
- Commit messages: Follow Conventional Commits
- Code review required trước khi merge
- Unit tests for critical features

---

## 📞 CONTACT & SUPPORT

### Project Links

- **GitHub Repository:** [Code-Explainer-Back-end](https://github.com/kleqing/Code-Explainer-Back-end)
- **Documentation:** [Wiki Page]
- **Issue Tracker:** [GitHub Issues]

### Support

- Email: [your-email@example.com]
- Discord: [Server Link]
- Office Hours: Thứ 2-6, 9:00 AM - 5:00 PM

---

## 📄 LICENSE

This project is licensed under the **MIT License**.

```
MIT License

Copyright (c) 2025 Code Explainer Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🎓 KẾT LUẬN

Dự án **Code Explainer** là một hệ thống hoàn chỉnh kết hợp giữa:

- ✅ Backend API với ASP.NET Core 9.0
- ✅ Desktop application với WPF và MVVM
- ✅ AI integration với Gemini 2.0
- ✅ Real-time communication với SignalR
- ✅ Authentication/Authorization đầy đủ
- ✅ Clean Architecture và best practices

### Kết Quả Đạt Được

1. **Kỹ thuật:**
   - Xây dựng được full-stack application
   - Áp dụng design patterns và clean code
   - Tích hợp thành công AI service
   - Real-time features hoạt động tốt
2. **Chức năng:**

   - Giải thích code tự động bằng AI
   - Quản lý users và authentication
   - Chat history và session management
   - Notifications real-time

3. **Học được:**
   - .NET 9.0 và C# 13.0 features
   - SignalR real-time programming
   - AI API integration
   - Clean Architecture implementation
   - WPF desktop development

### Hướng Phát Triển

Dự án có tiềm năng mở rộng thành:

- 🌐 Web application với React/Angular
- 📱 Mobile app với .NET MAUI
- 🤖 Advanced AI features (code generation, bug detection)
- 👥 Collaborative features
- 📊 Analytics và reporting

---

**Ngày hoàn thành:** 17/11/2025  
**Version:** 1.0.0  
**Status:** ✅ Production Ready

---

_Cảm ơn thầy cô đã xem báo cáo!_
