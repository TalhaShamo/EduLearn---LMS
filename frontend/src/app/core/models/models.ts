// ─── Auth Models ─────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: 'Student' | 'Instructor';
}

export interface AuthResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: User;
}

export interface User {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  profileImageUrl?: string;
  isVerified: boolean;
}

export type UserRole = 'Student' | 'Instructor' | 'Admin' | 'CorporateManager';

// ─── Course Models ────────────────────────────────────────────────────────────
export interface Course {
  courseId: string;
  title: string;
  slug: string;
  subtitle?: string;
  description: string;
  instructorId: string;
  instructorName: string;
  instructorImageUrl?: string;
  categoryId: string;
  categoryName: string;
  level: CourseLevel;
  language: string;
  thumbnailUrl?: string;
  price: number;
  status: CourseStatus;
  durationMinutes: number;
  enrollmentCount: number;
  averageRating: number;
  reviewCount: number;
  tags: string[];
  sections?: Section[];
  learningObjectives?: string[];
  createdAt: string;
  updatedAt: string;
}

export type CourseLevel = 'Beginner' | 'Intermediate' | 'Advanced';
export type CourseStatus = 'Draft' | 'PendingReview' | 'Published' | 'Archived' | 'ChangesRequested';

export interface Section {
  sectionId: string;
  courseId: string;
  title: string;
  sortOrder: number;
  lessons: Lesson[];
}

export interface Lesson {
  lessonId: string;
  sectionId: string;
  title: string;
  lessonType: LessonType;
  contentUrl?: string;
  richContent?: string;
  durationSeconds: number;
  isFreePreview: boolean;
  sortOrder: number;
  isPublished: boolean;
  isCompleted?: boolean;
}

export type LessonType = 'Video' | 'Article' | 'Quiz' | 'Assignment';

// ─── Enrollment Models ────────────────────────────────────────────────────────
export interface Enrollment {
  enrollmentId: string;
  courseId: string;
  course?: Course;
  // Flat course properties from enriched API response
  courseTitle?: string;
  courseSlug?: string;
  courseThumbnailUrl?: string;
  courseLevel?: string;
  courseCategory?: string;
  enrolledAt: string;
  completionPercentage: number;
  isCompleted: boolean;
  completedAt?: string;
}

export interface LessonProgress {
  progressId: string;
  lessonId: string;
  status: 'NotStarted' | 'InProgress' | 'Completed';
  watchedSeconds: number;
  completedAt?: string;
  lastAccessedAt?: string;
}

// ─── Quiz Models ──────────────────────────────────────────────────────────────
export interface Quiz {
  quizId: string;
  lessonId: string;
  title: string;
  timeLimitSeconds?: number;
  passingScore: number;
  maxAttempts?: number;
  randomizeQuestions: boolean;
  questions: Question[];
}

export interface Question {
  questionId: string;
  text: string;
  questionType: QuestionType;
  points: number;
  options?: QuestionOption[];
  explanation?: string;
}

export interface QuestionOption {
  optionId: string;
  text: string;
}

export type QuestionType = 'MCQ' | 'TrueFalse' | 'FillBlank' | 'ShortAnswer';

export interface QuizAttempt {
  attemptId: string;
  quizId: string;
  status: 'InProgress' | 'Submitted' | 'Graded';
  score?: number;
  maxScore: number;
  isPassed?: boolean;
  startedAt: string;
  submittedAt?: string;
}

export interface QuizResult {
  attemptId: string;
  score: number;
  maxScore: number;
  passed: boolean;
  pendingManualGrade: boolean;
  answers: AnswerResult[];
}

export interface AnswerResult {
  questionId: string;
  questionText: string;
  isCorrect?: boolean;
  earnedPoints: number;
  maxPoints: number;
  explanation?: string;
}

// ─── Certificate Models ───────────────────────────────────────────────────────
export interface Certificate {
  certificateId: string;
  courseId: string;
  courseName: string;
  instructorName: string;
  issuedAt: string;
  pdfUrl: string;
  isRevoked: boolean;
}

export interface CertificateVerification {
  isValid: boolean;
  studentName?: string;
  courseName?: string;
  completionDate?: string;
  instructorName?: string;
}

// ─── Payment Models ───────────────────────────────────────────────────────────
export interface CreateOrderRequest {
  courseId: string;
  amount: number;
  currency: string;
}

export interface CreateOrderResponse {
  orderId: string;
  razorpayOrderId: string;
  amount: number;
  currency: string;
  razorpayKeyId: string;
}

export interface VerifyPaymentRequest {
  orderId: string;
  razorpayPaymentId: string;
  razorpaySignature: string;
}

// ─── Discussion Models ────────────────────────────────────────────────────────
export interface DiscussionPost {
  postId: string;
  courseId: string;
  authorId: string;
  authorName: string;
  authorImageUrl?: string;
  title: string;
  body: string;
  createdAt: string;
  replyCount: number;
  replies?: DiscussionReply[];
}

export interface DiscussionReply {
  replyId: string;
  postId: string;
  authorId: string;
  authorName: string;
  authorImageUrl?: string;
  body: string;
  upvotes: number;
  isAcceptedAnswer: boolean;
  hasUpvoted: boolean;
  createdAt: string;
}

// ─── Admin Models ─────────────────────────────────────────────────────────────
export interface AdminUser {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  isVerified: boolean;
  isBanned: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface PlatformStats {
  totalUsers: number;
  totalCourses: number;
  totalEnrollments: number;
  totalRevenue: number;
  activeCoursesCount: number;
  dailyActiveUsers: number;
}

// ─── API Wrapper ──────────────────────────────────────────────────────────────
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
