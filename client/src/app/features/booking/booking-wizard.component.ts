import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BookingDetailsDto,
  BrandDto,
  ModelDto,
  ModelServiceDto,
  SlotDto,
} from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-booking-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './booking-wizard.component.html',
  styleUrl: './booking-wizard.component.scss',
})
export class BookingWizardComponent implements OnInit, OnDestroy {
  readonly api = inject(ApiService);
  readonly i18n = inject(I18nService);
  readonly theme = inject(ThemeService);

  // Wizard step: 1 = Vehicle, 2 = Services, 3 = Date & Time, 4 = Customer Details, 5 = Confirmation
  readonly currentStep = signal<number>(1);

  // Step 1: Catalog
  readonly brands = signal<BrandDto[]>([]);
  readonly models = signal<ModelDto[]>([]);
  readonly searchQuery = signal<string>('');
  readonly selectedBrand = signal<BrandDto | null>(null);
  readonly selectedModel = signal<ModelDto | null>(null);
  readonly loadingCatalog = signal<boolean>(false);

  readonly filteredBrands = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return this.brands();
    return this.brands().filter((b) => b.name.toLowerCase().includes(q));
  });

  // Step 2: Services
  readonly availableServices = signal<ModelServiceDto[]>([]);
  readonly selectedServiceIds = signal<number[]>([]);
  readonly loadingServices = signal<boolean>(false);

  readonly selectedServices = computed(() => {
    const ids = new Set(this.selectedServiceIds());
    return this.availableServices().filter((s) => ids.has(s.serviceId));
  });

  readonly totalPrice = computed(() =>
    this.selectedServices().reduce((sum, s) => sum + s.price, 0)
  );

  readonly totalDurationMinutes = computed(() =>
    this.selectedServices().reduce((sum, s) => sum + s.durationMinutes, 0)
  );

  // Step 3: Date & Slots
  readonly upcomingDates = signal<{ iso: string; dayNameAr: string; dayNameEn: string; dayNumber: string; monthName: string }[]>([]);
  readonly selectedDate = signal<string>('');
  readonly slots = signal<SlotDto[]>([]);
  readonly isDayClosed = signal<boolean>(false);
  readonly closedReason = signal<string | null>(null);
  readonly selectedSlot = signal<SlotDto | null>(null);
  readonly loadingSlots = signal<boolean>(false);

  // Step 4: Customer Form
  readonly customerName = signal<string>('');
  readonly customerPhone = signal<string>('');
  readonly customerEmail = signal<string>('');
  readonly modelYear = signal<number | null>(null);
  readonly plateNumber = signal<string>('');
  readonly customerNotes = signal<string>('');
  readonly submitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  // Step 5: Confirmed Booking + Kiosk Auto-reset Timer
  readonly confirmedBooking = signal<BookingDetailsDto | null>(null);
  readonly kioskCountdown = signal<number>(45);
  private countdownTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.buildUpcomingDates();
    this.loadBrands();
  }

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  private buildUpcomingDates(): void {
    const list: { iso: string; dayNameAr: string; dayNameEn: string; dayNumber: string; monthName: string }[] = [];
    const arDays = ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'];
    const enDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    const today = new Date();
    for (let i = 0; i < 14; i++) {
      const d = new Date(today);
      d.setDate(today.getDate() + i);
      const yyyy = d.getFullYear();
      const mm = String(d.getMonth() + 1).padStart(2, '0');
      const dd = String(d.getDate()).padStart(2, '0');
      const iso = `${yyyy}-${mm}-${dd}`;
      list.push({
        iso,
        dayNameAr: arDays[d.getDay()],
        dayNameEn: enDays[d.getDay()],
        dayNumber: dd,
        monthName: mm,
      });
    }
    this.upcomingDates.set(list);
    if (list.length > 0) {
      this.selectedDate.set(list[0].iso);
    }
  }

  loadBrands(): void {
    this.loadingCatalog.set(true);
    this.api.getBrands().subscribe({
      next: (data) => {
        this.brands.set(data);
        this.loadingCatalog.set(false);
        if (data.length > 0 && !this.selectedBrand()) {
          this.selectBrand(data[0]);
        }
      },
      error: () => {
        this.loadingCatalog.set(false);
      },
    });
  }

  selectBrand(brand: BrandDto): void {
    this.selectedBrand.set(brand);
    this.selectedModel.set(null);
    this.loadingCatalog.set(true);
    this.api.getModelsByBrand(brand.id).subscribe({
      next: (models) => {
        this.models.set(models);
        this.loadingCatalog.set(false);
      },
      error: () => {
        this.loadingCatalog.set(false);
      },
    });
  }

  selectModel(model: ModelDto): void {
    this.selectedModel.set(model);
    this.selectedServiceIds.set([]);
    this.errorMessage.set(null);
    this.loadServicesForModel(model.id);
    this.currentStep.set(2);
  }

  loadServicesForModel(modelId: number): void {
    this.loadingServices.set(true);
    this.api.getServicesForModel(modelId).subscribe({
      next: (services) => {
        this.availableServices.set(services);
        this.loadingServices.set(false);
      },
      error: () => {
        this.loadingServices.set(false);
      },
    });
  }

  isServiceSelected(serviceId: number): boolean {
    return this.selectedServiceIds().includes(serviceId);
  }

  toggleService(serviceId: number): void {
    const current = this.selectedServiceIds();
    if (current.includes(serviceId)) {
      this.selectedServiceIds.set(current.filter((id) => id !== serviceId));
    } else {
      this.selectedServiceIds.set([...current, serviceId]);
    }
  }

  goToStep(step: number): void {
    this.errorMessage.set(null);
    if (step === 3) {
      this.loadSlotsForDate(this.selectedDate());
    }
    this.currentStep.set(step);
  }

  selectDate(dateIso: string): void {
    this.selectedDate.set(dateIso);
    this.selectedSlot.set(null);
    this.loadSlotsForDate(dateIso);
  }

  loadSlotsForDate(dateIso: string): void {
    const model = this.selectedModel();
    const sIds = this.selectedServiceIds();
    if (!model || sIds.length === 0 || !dateIso) return;

    this.loadingSlots.set(true);
    this.slots.set([]);
    this.isDayClosed.set(false);
    this.closedReason.set(null);

    this.api.getAvailableSlots(dateIso, model.id, sIds).subscribe({
      next: (res) => {
        this.isDayClosed.set(res.isClosed);
        this.closedReason.set(res.closedReason ?? null);
        this.slots.set(res.slots);
        this.loadingSlots.set(false);
      },
      error: (err) => {
        this.loadingSlots.set(false);
        this.errorMessage.set(err?.error?.message || 'تعذر تحميل المواعيد المتاحة.');
      },
    });
  }

  selectSlot(slot: SlotDto): void {
    this.selectedSlot.set(slot);
    this.errorMessage.set(null);
  }

  // Touch Keypad helper for Kiosk phone number entry
  appendPhoneDigit(digit: string): void {
    if (this.customerPhone().length < 11) {
      this.customerPhone.set(this.customerPhone() + digit);
    }
  }

  backspacePhone(): void {
    const cur = this.customerPhone();
    if (cur.length > 0) {
      this.customerPhone.set(cur.slice(0, -1));
    }
  }

  clearPhone(): void {
    this.customerPhone.set('');
  }

  formatTime(timeStr: string): string {
    return timeStr ? timeStr.slice(0, 5) : '';
  }

  submitBooking(): void {
    const model = this.selectedModel();
    const slot = this.selectedSlot();
    if (!model || !slot || this.selectedServiceIds().length === 0) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.api
      .createBooking({
        modelId: model.id,
        modelYear: this.modelYear(),
        plateNumber: this.plateNumber() || null,
        serviceIds: this.selectedServiceIds(),
        date: this.selectedDate(),
        time: slot.time,
        customerName: this.customerName(),
        customerPhone: this.customerPhone(),
        customerEmail: this.customerEmail() || null,
        customerNotes: this.customerNotes() || null,
      })
      .subscribe({
        next: (booking) => {
          this.submitting.set(false);
          this.confirmedBooking.set(booking);
          this.currentStep.set(5);
          if (this.theme.kioskMode()) {
            this.startKioskCountdown();
          }
        },
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(
            err?.error?.message || 'حدث خطأ أثناء تأكيد الحجز. يرجى مراجعة البيانات.'
          );
        },
      });
  }

  printTicket(): void {
    window.print();
  }

  resetWizard(): void {
    this.clearCountdown();
    this.selectedModel.set(null);
    this.selectedServiceIds.set([]);
    this.selectedSlot.set(null);
    this.customerName.set('');
    this.customerPhone.set('');
    this.customerEmail.set('');
    this.modelYear.set(null);
    this.plateNumber.set('');
    this.customerNotes.set('');
    this.confirmedBooking.set(null);
    this.errorMessage.set(null);
    this.currentStep.set(1);
  }

  private startKioskCountdown(): void {
    this.clearCountdown();
    this.kioskCountdown.set(45);
    this.countdownTimer = setInterval(() => {
      const next = this.kioskCountdown() - 1;
      if (next <= 0) {
        this.resetWizard();
      } else {
        this.kioskCountdown.set(next);
      }
    }, 1000);
  }

  private clearCountdown(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }
}

