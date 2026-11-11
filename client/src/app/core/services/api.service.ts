import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminBrandDto,
  AdminCategoryDto,
  AdminHolidayDto,
  AdminLoginResponse,
  AdminModelDto,
  AdminServiceDto,
  AdminServicePriceDto,
  AdminShopSettingDto,
  AdminWorkingHourDto,
  AvailableSlotsResponse,
  BookingDetailsDto,
  BookingStatus,
  BrandDto,
  CreateBookingRequest,
  DashboardSummaryDto,
  ModelDto,
  ModelServiceDto,
  QuoteResponse,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5067/api';

  readonly adminToken = signal<string | null>(localStorage.getItem('cs_admin_token'));

  private getAuthHeaders(): HttpHeaders {
    const token = this.adminToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  setAdminToken(token: string | null): void {
    this.adminToken.set(token);
    if (token) {
      localStorage.setItem('cs_admin_token', token);
    } else {
      localStorage.removeItem('cs_admin_token');
    }
  }

  // Public Catalog
  getBrands(): Observable<BrandDto[]> {
    return this.http.get<BrandDto[]>(`${this.baseUrl}/brands`);
  }

  getModelsByBrand(brandId: number): Observable<ModelDto[]> {
    return this.http.get<ModelDto[]>(`${this.baseUrl}/brands/${brandId}/models`);
  }

  getServicesForModel(modelId: number): Observable<ModelServiceDto[]> {
    return this.http.get<ModelServiceDto[]>(`${this.baseUrl}/models/${modelId}/services`);
  }

  // Quote & Slots
  calculateQuote(modelId: number, serviceIds: number[]): Observable<QuoteResponse> {
    return this.http.post<QuoteResponse>(`${this.baseUrl}/quote`, { modelId, serviceIds });
  }

  getAvailableSlots(date: string, modelId: number, serviceIds: number[]): Observable<AvailableSlotsResponse> {
    let params = new HttpParams().set('date', date).set('modelId', modelId);
    serviceIds.forEach((id) => {
      params = params.append('serviceIds', id);
    });
    return this.http.get<AvailableSlotsResponse>(`${this.baseUrl}/slots`, { params });
  }

  // Customer Bookings
  createBooking(request: CreateBookingRequest): Observable<BookingDetailsDto> {
    return this.http.post<BookingDetailsDto>(`${this.baseUrl}/bookings`, request);
  }

  getBookingByReference(reference: string): Observable<BookingDetailsDto> {
    return this.http.get<BookingDetailsDto>(`${this.baseUrl}/bookings/${encodeURIComponent(reference.trim())}`);
  }

  cancelBooking(reference: string): Observable<BookingDetailsDto> {
    return this.http.post<BookingDetailsDto>(
      `${this.baseUrl}/bookings/${encodeURIComponent(reference.trim())}/cancel`,
      {}
    );
  }

  // Admin Auth
  adminLogin(username: string, password: string): Observable<AdminLoginResponse> {
    return this.http.post<AdminLoginResponse>(`${this.baseUrl}/admin/auth/login`, { username, password });
  }

  // Admin Dashboard & Bookings
  getDashboard(): Observable<DashboardSummaryDto> {
    return this.http.get<DashboardSummaryDto>(`${this.baseUrl}/admin/dashboard`, {
      headers: this.getAuthHeaders(),
    });
  }

  getAdminBookings(status?: BookingStatus | null, search?: string): Observable<BookingDetailsDto[]> {
    let params = new HttpParams();
    if (status !== undefined && status !== null) {
      params = params.set('status', status);
    }
    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<BookingDetailsDto[]>(`${this.baseUrl}/admin/bookings`, {
      headers: this.getAuthHeaders(),
      params,
    });
  }

  changeBookingStatus(bookingId: number, status: BookingStatus, internalNotes?: string): Observable<BookingDetailsDto> {
    return this.http.patch<BookingDetailsDto>(
      `${this.baseUrl}/admin/bookings/${bookingId}/status`,
      { status, internalNotes },
      { headers: this.getAuthHeaders() }
    );
  }

  // Admin Catalog & Pricing
  getAdminCategories(): Observable<AdminCategoryDto[]> {
    return this.http.get<AdminCategoryDto[]>(`${this.baseUrl}/admin/categories`, {
      headers: this.getAuthHeaders(),
    });
  }

  createCategory(name: string, sortOrder: number): Observable<AdminCategoryDto> {
    return this.http.post<AdminCategoryDto>(
      `${this.baseUrl}/admin/categories`,
      { name, sortOrder },
      { headers: this.getAuthHeaders() }
    );
  }

  getAdminBrands(): Observable<AdminBrandDto[]> {
    return this.http.get<AdminBrandDto[]>(`${this.baseUrl}/admin/brands`, {
      headers: this.getAuthHeaders(),
    });
  }

  createBrand(name: string, isActive = true): Observable<AdminBrandDto> {
    return this.http.post<AdminBrandDto>(
      `${this.baseUrl}/admin/brands`,
      { name, isActive },
      { headers: this.getAuthHeaders() }
    );
  }

  getAdminModels(): Observable<AdminModelDto[]> {
    return this.http.get<AdminModelDto[]>(`${this.baseUrl}/admin/models`, {
      headers: this.getAuthHeaders(),
    });
  }

  createModel(payload: {
    brandId: number;
    categoryId: number;
    name: string;
    yearFrom?: number | null;
    yearTo?: number | null;
    isActive: boolean;
  }): Observable<AdminModelDto> {
    return this.http.post<AdminModelDto>(`${this.baseUrl}/admin/models`, payload, {
      headers: this.getAuthHeaders(),
    });
  }

  getAdminServices(): Observable<AdminServiceDto[]> {
    return this.http.get<AdminServiceDto[]>(`${this.baseUrl}/admin/services`, {
      headers: this.getAuthHeaders(),
    });
  }

  createService(payload: {
    name: string;
    description?: string | null;
    isActive: boolean;
    sortOrder: number;
  }): Observable<AdminServiceDto> {
    return this.http.post<AdminServiceDto>(`${this.baseUrl}/admin/services`, payload, {
      headers: this.getAuthHeaders(),
    });
  }

  getAdminPrices(): Observable<AdminServicePriceDto[]> {
    return this.http.get<AdminServicePriceDto[]>(`${this.baseUrl}/admin/prices`, {
      headers: this.getAuthHeaders(),
    });
  }

  upsertServicePrice(payload: {
    serviceId: number;
    categoryId: number;
    price: number;
    durationMinutes: number;
  }): Observable<AdminServicePriceDto> {
    return this.http.put<AdminServicePriceDto>(`${this.baseUrl}/admin/prices`, payload, {
      headers: this.getAuthHeaders(),
    });
  }

  bulkAdjustPrices(percentage: number, categoryId?: number | null): Observable<{ updatedCount: number; message: string }> {
    return this.http.post<{ updatedCount: number; message: string }>(
      `${this.baseUrl}/admin/prices/bulk-adjust`,
      { percentage, categoryId: categoryId || null },
      { headers: this.getAuthHeaders() }
    );
  }

  getWorkingHours(): Observable<AdminWorkingHourDto[]> {
    return this.http.get<AdminWorkingHourDto[]>(`${this.baseUrl}/admin/working-hours`, {
      headers: this.getAuthHeaders(),
    });
  }

  updateWorkingHours(hours: { day: number; openTime: string; closeTime: string; isClosed: boolean }[]): Observable<AdminWorkingHourDto[]> {
    return this.http.put<AdminWorkingHourDto[]>(`${this.baseUrl}/admin/working-hours`, hours, {
      headers: this.getAuthHeaders(),
    });
  }

  getHolidays(): Observable<AdminHolidayDto[]> {
    return this.http.get<AdminHolidayDto[]>(`${this.baseUrl}/admin/holidays`, {
      headers: this.getAuthHeaders(),
    });
  }

  createHoliday(date: string, reason?: string): Observable<AdminHolidayDto> {
    return this.http.post<AdminHolidayDto>(
      `${this.baseUrl}/admin/holidays`,
      { date, reason },
      { headers: this.getAuthHeaders() }
    );
  }

  deleteHoliday(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/admin/holidays/${id}`, {
      headers: this.getAuthHeaders(),
    });
  }

  getShopSettings(): Observable<AdminShopSettingDto> {
    return this.http.get<AdminShopSettingDto>(`${this.baseUrl}/admin/settings`, {
      headers: this.getAuthHeaders(),
    });
  }

  updateShopSettings(settings: {
    maxConcurrentBookings: number;
    slotIntervalMinutes: number;
    minHoursBeforeBooking: number;
    maxDaysAhead: number;
  }): Observable<AdminShopSettingDto> {
    return this.http.put<AdminShopSettingDto>(`${this.baseUrl}/admin/settings`, settings, {
      headers: this.getAuthHeaders(),
    });
  }
}

