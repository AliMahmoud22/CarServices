export interface BrandDto {
  id: number;
  name: string;
}

export interface ModelDto {
  id: number;
  brandId: number;
  categoryId: number;
  categoryName: string;
  name: string;
  yearFrom?: number | null;
  yearTo?: number | null;
}

export interface ModelServiceDto {
  serviceId: number;
  name: string;
  description?: string | null;
  price: number;
  durationMinutes: number;
}

export interface QuoteItemDto {
  serviceId: number;
  serviceName: string;
  price: number;
  durationMinutes: number;
}

export interface QuoteResponse {
  modelId: number;
  items: QuoteItemDto[];
  totalPrice: number;
  totalDurationMinutes: number;
}

export interface SlotDto {
  time: string;
  startAtUtc: string;
  endAtUtc: string;
  isAvailable: boolean;
}

export interface AvailableSlotsResponse {
  date: string;
  isClosed: boolean;
  closedReason?: string | null;
  totalDurationMinutes: number;
  slots: SlotDto[];
}

export interface CreateBookingRequest {
  modelId: number;
  modelYear?: number | null;
  plateNumber?: string | null;
  serviceIds: number[];
  date: string;
  time: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string | null;
  customerNotes?: string | null;
}

export type BookingStatus = 0 | 1 | 2 | 3 | 4;

export interface BookingDetailsDto {
  id: number;
  reference: string;
  status: BookingStatus;
  statusName: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string | null;
  vehicleModelId: number;
  brandName: string;
  modelName: string;
  categoryName: string;
  modelYear?: number | null;
  plateNumber?: string | null;
  date: string;
  startTime: string;
  endTime: string;
  startAtUtc: string;
  endAtUtc: string;
  totalPrice: number;
  totalDurationMinutes: number;
  customerNotes?: string | null;
  createdAtUtc: string;
  items: QuoteItemDto[];
  whatsAppShareUrl: string;
}

// Admin Models
export interface AdminLoginResponse {
  token: string;
  expiresAtUtc: string;
  username: string;
}

export interface DashboardSummaryDto {
  todayBookingsCount: number;
  pendingBookingsCount: number;
  confirmedBookingsCount: number;
  completedBookingsCount: number;
  cancelledBookingsCount: number;
  completedRevenueTotal: number;
}

export interface AdminCategoryDto {
  id: number;
  name: string;
  sortOrder: number;
  modelsCount: number;
}

export interface AdminBrandDto {
  id: number;
  name: string;
  isActive: boolean;
  modelsCount: number;
}

export interface AdminModelDto {
  id: number;
  brandId: number;
  brandName: string;
  categoryId: number;
  categoryName: string;
  name: string;
  yearFrom?: number | null;
  yearTo?: number | null;
  isActive: boolean;
}

export interface AdminServiceDto {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  sortOrder: number;
}

export interface AdminServicePriceDto {
  id: number;
  serviceId: number;
  serviceName: string;
  categoryId: number;
  categoryName: string;
  price: number;
  durationMinutes: number;
}

export interface AdminWorkingHourDto {
  id: number;
  day: number;
  dayNameAr: string;
  openTime: string;
  closeTime: string;
  isClosed: boolean;
}

export interface AdminHolidayDto {
  id: number;
  date: string;
  reason?: string | null;
}

export interface AdminShopSettingDto {
  id: number;
  maxConcurrentBookings: number;
  slotIntervalMinutes: number;
  minHoursBeforeBooking: number;
  maxDaysAhead: number;
}

