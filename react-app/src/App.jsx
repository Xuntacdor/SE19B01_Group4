import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Authorization from "./Hook/Authorization.jsx";
import Login from "./Pages/Authenciation/Login.jsx";
import ForgotPassword from "./Pages/Authenciation/ForgotPassword.jsx";
import VerifyOtp from "./Pages/Authenciation/VerifyOtp.jsx";
import ResetPassword from "./Pages/Authenciation/ResetPassword.jsx";
import Home from "./Pages/Dashboard/DashboardUser.jsx";
import Dictionary from "./Pages/Dictionary/Dictionary.jsx";
import Forum from "./Pages/Forum/Forum.jsx";
import ReadingPage from "./Pages/Reading/ReadingPage.jsx";
import ListeningPage from "./Pages/Listening/ListeningPage.jsx";
import WritingPage from "./Pages/Writing/WritingPage.jsx";
import CreatePost from "./Pages/Forum/CreatePost.jsx";
import EditPost from "./Pages/Forum/EditPost.jsx";
import PostDetail from "./Pages/Forum/PostDetail.jsx";
import Profile from "./Pages/Profile/Profile.jsx";
import ExamManagement from "./Pages/Admin/ExamManagement.jsx";
import AdminDashboard from "./Pages/Admin/AdminDashBoard.jsx";
import AddReading from "./Pages/Admin/AddReading.jsx";
import AddListening from "./Pages/Admin/AddListening.jsx";
import AddWriting from "./Pages/Admin/AddWriting.jsx";
import AddSpeaking from "./Pages/Admin/AddSpeaking.jsx";
import ModeratorDashboard from "./Pages/Moderator/ModeratorDashboard.jsx";
import ModeratorProfile from "./Pages/Moderator/ModeratorProfile.jsx";
import TagManagement from "./Pages/Moderator/TagManagement.jsx";
import WritingTestPage from "./Pages/Writing/WritingTestPage.jsx";
import ReadingExamPage from "./Pages/Reading/ReadingExamPage.jsx";
import ListeningExamPage from "./Pages/Listening/ListeningExamPage.jsx";
import TransactionList from "./Pages/Transactions/TransactionList.jsx";
import TransactionDetail from "./Pages/Transactions/TransactionDetail.jsx";
import PaymentTab from "./Pages/Profile/Tabs/PaymentTab.jsx";
import AdminVipPlans from "./Pages/Admin/AdminVipPlans.jsx";
import UserManagement from "./Pages/Admin/UserManagement.jsx";
import WritingResultPage from "./Pages/Writing/WritingResultPage.jsx";
import SpeakingPage from "./Pages/Speaking/SpeakingPage.jsx";
import SpeakingTestPage from "./Pages/Speaking/SpeakingTestPage.jsx";
import ReadingResultPage from "./Pages/Reading/ReadingResultPage.jsx";
import ListeningResultPage from "./Pages/Listening/ListeningResultPage.jsx";
import VipPlans from "./Pages/Transactions/VipPlans.jsx";
import PaymentSuccess from "./Pages/Transactions/PaymentSuccess.jsx";
import SpeakingResultPage from "./Pages/Speaking/SpeakingResultPage.jsx";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* ========== AUTH ROUTES ========== */}
        <Route path="/" element={<Login />} />
        <Route path="/login" element={<Login />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/verify-otp" element={<VerifyOtp />} />
        <Route path="/reset-password" element={<ResetPassword />} />

        {/* ========== USER AREA ========== */}
        <Route path="/home" element={<Home />} />
        <Route path="/dictionary" element={<Dictionary />} />
        <Route path="/forum" element={<Forum />} />
        <Route path="/create-post" element={<CreatePost />} />
        <Route path="/edit-post/:postId" element={<EditPost />} />
        <Route path="/post/:postId" element={<PostDetail />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/profile/payment" element={<PaymentTab />} />

        {/* ========== EXAM & RESULT ========== */}
        <Route path="/reading" element={<ReadingPage />} />
        <Route path="/listening" element={<ListeningPage />} />
        <Route path="/writing" element={<WritingPage />} />
        <Route path="/speaking" element={<SpeakingPage />} />
        <Route path="/reading/test" element={<ReadingExamPage />} />
        <Route path="/listening/test" element={<ListeningExamPage />} />
        <Route path="/writing/test" element={<WritingTestPage />} />
        <Route path="/writing/result" element={<WritingResultPage />} />
        <Route path="/speaking/test" element={<SpeakingTestPage />} />
        <Route path="/speaking/result" element={<SpeakingResultPage />} />

        <Route path="/reading/result" element={<ReadingResultPage />} />
        <Route path="/listening/result" element={<ListeningResultPage />} />

        {/* ========== ADMIN AREA ========== */}
        <Route path="/admin" element={<Authorization allow={["admin"]} />}>
          <Route path="dashboard" element={<AdminDashboard />} />
          <Route path="users" element={<UserManagement />} />
          <Route path="exam" element={<ExamManagement />} />
          <Route path="exam/add-reading" element={<AddReading />} />
          <Route path="exam/add-listening" element={<AddListening />} />
          <Route path="exam/add-writing" element={<AddWriting />} />
          <Route path="exam/add-speaking" element={<AddSpeaking />} />
          <Route path="transactions" element={<TransactionList />} />
          <Route path="transactions/:id" element={<TransactionDetail />} />
          <Route path="vip-plans" element={<AdminVipPlans />} />
        </Route>

        {/* ========== MODERATOR AREA ========== */}
        <Route path="/moderator" element={<Authorization allow={["admin", "moderator"]} />}>
          <Route path="dashboard" element={<ModeratorDashboard />} />
          <Route path="profile" element={<ModeratorProfile />} />
          <Route path="tags" element={<TagManagement />} />
        </Route>

        {/* ========== STRIPE VIP AREA ========== */}
        <Route path="/vipplans" element={<VipPlans />} />
        <Route path="/payment-success" element={<PaymentSuccess />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
