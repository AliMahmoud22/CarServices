import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminBrandDto,
  AdminCategoryDto,
  AdminHolidayDto,
  AdminModelDto,
  AdminServiceDto,
  AdminServicePriceDto,
  AdminShopSettingDto,
  AdminWorkingHourDto,
  BookingDetailsDto,
  BookingStatus,
  DashboardSummaryDto,
} from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent implements OnInit {
  readonly api = inject(ApiService);
  readonly i18n = inject(I18nService);

  // Login State
  readonly username = signal<string>('admin');
  readonly password = signal<string>('Admin@123');
  readonly loginError = signal<string | null>(null);

  // Active Tab: 'bookings' | 'catalog' | 'services' | 'settings'
  readonly activeTab = signal<'bookings' | 'catalog' | 'services' | 'settings'>('bookings');
  readonly feedbackMessage = signal<string | null>(null);

  // Dashboard & Bookings
  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly bookings = signal<BookingDetailsDto[]>([]);
  readonly statusFilter = signal<BookingStatus | null>(null);
  readonly searchFilter = signal<string>('');

  // Catalog
  readonly categories = signal<AdminCategoryDto[]>([]);
  readonly brands = signal<AdminBrandDto[]>([]);
  readonly models = signal<AdminModelDto[]>([]);
  readonly newBrandName = signal<string>('');
  readonly newModelName = signal<string>('');
  readonly newModelBrandId = signal<number>(1);
  readonly newModelCategoryId = signal<number>(2);

  // Services & Prices
  readonly services = signal<AdminServiceDto[]>([]);
  readonly prices = signal<AdminServicePriceDto[]>([]);
  readonly newServiceName = signal<string>('');
  readonly newServiceDesc = signal<string>('');
  readonly editPriceServiceId = signal<number>(1);
  readonly editPriceCategoryId = signal<number>(2);
  readonly editPriceAmount = signal<number>(500);
  readonly editPriceDuration = signal<number>(30);
  readonly bulkPercentage = signal<number>(10);
  readonly bulkCategoryId = signal<number | null>(null);

  // Settings & Hours
  readonly workingHours = signal<AdminWorkingHourDto[]>([]);
  readonly holidays = signal<AdminHolidayDto[]>([]);
  readonly newHolidayDate = signal<string>('');
  readonly newHolidayReason = signal<string>('');
  readonly shopSettings = signal<AdminShopSettingDto>({
    id: 1,
    maxConcurrentBookings: 2,
    slotIntervalMinutes: 30,
    minHoursBeforeBooking: 2,
    maxDaysAhead: 30,
  });

  ngOnInit(): void {
    if (this.api.adminToken()) {
      this.loadAllAdminData();
    }
  }

  login(): void {
    this.loginError.set(null);
    this.api.adminLogin(this.username(), this.password()).subscribe({
      next: (res) => {
        this.api.setAdminToken(res.token);
        this.loadAllAdminData();
      },
      error: (err) => {
        this.loginError.set(err?.error?.message || 'فشل تسجيل الدخول.');
      },
    });
  }

  logout(): void {
    this.api.setAdminToken(null);
  }

  loadAllAdminData(): void {
    this.loadDashboardAndBookings();
    this.loadCatalog();
    this.loadServicesAndPrices();
    this.loadSettingsAndHours();
  }

  loadDashboardAndBookings(): void {
    this.api.getDashboard().subscribe({
      next: (s) => this.summary.set(s),
      error: (err) => {
        if (err.status === 401) this.logout();
      },
    });
    this.api.getAdminBookings(this.statusFilter(), this.searchFilter()).subscribe({
      next: (b) => this.bookings.set(b),
    });
  }

  updateBookingStatus(bookingId: number, status: BookingStatus): void {
    this.api.changeBookingStatus(bookingId, status).subscribe({
      next: () => {
        this.showFeedback('تم تحديث حالة الحجز بنجاح');
        this.loadDashboardAndBookings();
      },
    });
  }

  loadCatalog(): void {
    this.api.getAdminCategories().subscribe((c) => this.categories.set(c));
    this.api.getAdminBrands().subscribe((b) => this.brands.set(b));
    this.api.getAdminModels().subscribe((m) => this.models.set(m));
  }

  addBrand(): void {
    const name = this.newBrandName().trim();
    if (!name) return;
    this.api.createBrand(name, true).subscribe({
      next: () => {
        this.newBrandName.set('');
        this.loadCatalog();
        this.showFeedback('تمت إضافة الماركة');
      },
    });
  }

  addModel(): void {
    const name = this.newModelName().trim();
    if (!name) return;
    this.api
      .createModel({
        brandId: Number(this.newModelBrandId()),
        categoryId: Number(this.newModelCategoryId()),
        name,
        yearFrom: 2015,
        yearTo: 2026,
        isActive: true,
      })
      .subscribe({
        next: () => {
          this.newModelName.set('');
          this.loadCatalog();
          this.showFeedback('تمت إضافة الموديل');
        },
      });
  }

  loadServicesAndPrices(): void {
    this.api.getAdminServices().subscribe((s) => this.services.set(s));
    this.api.getAdminPrices().subscribe((p) => this.prices.set(p));
  }

  addService(): void {
    const name = this.newServiceName().trim();
    if (!name) return;
    this.api
      .createService({
        name,
        description: this.newServiceDesc() || null,
        isActive: true,
        sortOrder: this.services().length + 1,
      })
      .subscribe({
        next: () => {
          this.newServiceName.set('');
          this.newServiceDesc.set('');
          this.loadServicesAndPrices();
          this.showFeedback('تمت إضافة الخدمة');
        },
      });
  }

  saveServicePrice(): void {
    this.api
      .upsertServicePrice({
        serviceId: Number(this.editPriceServiceId()),
        categoryId: Number(this.editPriceCategoryId()),
        price: Number(this.editPriceAmount()),
        durationMinutes: Number(this.editPriceDuration()),
      })
      .subscribe({
        next: () => {
          this.loadServicesAndPrices();
          this.showFeedback('تم حفظ التسعيرة');
        },
      });
  }

  applyBulkPercentage(): void {
    this.api.bulkAdjustPrices(Number(this.bulkPercentage()), this.bulkCategoryId()).subscribe({
      next: (res) => {
        this.loadServicesAndPrices();
        this.showFeedback(res.message);
      },
    });
  }

  loadSettingsAndHours(): void {
    this.api.getWorkingHours().subscribe((h) => this.workingHours.set(h));
    this.api.getHolidays().subscribe((h) => this.holidays.set(h));
    this.api.getShopSettings().subscribe((s) => this.shopSettings.set(s));
  }

  saveShopSettings(): void {
    this.api.updateShopSettings(this.shopSettings()).subscribe({
      next: (s) => {
        this.shopSettings.set(s);
        this.showFeedback('تم حفظ إعدادات المركز');
      },
    });
  }

  saveWorkingHours(): void {
    const payload = this.workingHours().map((w) => ({
      day: w.day,
      openTime: w.openTime,
      closeTime: w.closeTime,
      isClosed: w.isClosed,
    }));
    this.api.updateWorkingHours(payload).subscribe({
      next: (updated) => {
        this.workingHours.set(updated);
        this.showFeedback('تم حفظ مواعيد العمل');
      },
    });
  }

  addHoliday(): void {
    if (!this.newHolidayDate()) return;
    this.api.createHoliday(this.newHolidayDate(), this.newHolidayReason()).subscribe({
      next: () => {
        this.newHolidayDate.set('');
        this.newHolidayReason.set('');
        this.loadSettingsAndHours();
        this.showFeedback('تمت إضافة الإجازة');
      },
    });
  }

  removeHoliday(id: number): void {
    this.api.deleteHoliday(id).subscribe({
      next: () => {
        this.loadSettingsAndHours();
      },
    });
  }

  private showFeedback(msg: string): void {
    this.feedbackMessage.set(msg);
    setTimeout(() => this.feedbackMessage.set(null), 3500);
  }
}

