import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BookingDetailsDto } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';

@Component({
  selector: 'app-booking-lookup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="lookup-container">
      <section class="lookup-card">
        <h2>🔍 {{ i18n.t('lookupTitle') }}</h2>
        <p class="subtitle">{{ i18n.t('lookupSubtitle') }}</p>

        <div class="search-row">
          <input
            type="text"
            dir="ltr"
            class="touch-input ref-input"
            [placeholder]="i18n.t('lookupPlaceholder')"
            [ngModel]="referenceCode()"
            (ngModelChange)="referenceCode.set($event)"
            (keyup.enter)="searchBooking()"
          />
          <button
            type="button"
            class="btn-primary btn-lg"
            [disabled]="loading() || !referenceCode().trim()"
            (click)="searchBooking()"
          >
            {{ i18n.t('lookupBtn') }}
          </button>
        </div>

        @if (errorMessage()) {
          <div class="alert-banner error mt-3">⚠️ {{ errorMessage() }}</div>
        }

        @if (booking(); as b) {
          <div class="result-box mt-4">
            <div class="result-header">
              <div>
                <span class="ref-tag" dir="ltr">{{ b.reference }}</span>
                <h3>{{ b.brandName }} — {{ b.modelName }} ({{ b.categoryName }})</h3>
              </div>
              <span class="status-badge" [attr.data-status]="b.status">
                {{ b.statusName }}
              </span>
            </div>

            <div class="result-grid">
              <div>
                <span class="lbl">{{ i18n.t('customerName') }}:</span>
                <strong>{{ b.customerName }} ({{ b.customerPhone }})</strong>
              </div>
              <div>
                <span class="lbl">{{ i18n.t('appointmentLabel') }}:</span>
                <strong>{{ b.date }} | {{ b.startTime.slice(0, 5) }} - {{ b.endTime.slice(0, 5) }}</strong>
              </div>
            </div>

            <ul class="items-list">
              @for (item of b.items; track item.serviceId) {
                <li>
                  <span>{{ item.serviceName }} ({{ item.durationMinutes }} {{ i18n.t('durationMin') }})</span>
                  <strong>{{ item.price }} {{ i18n.t('currency') }}</strong>
                </li>
              }
            </ul>

            <div class="result-footer">
              <div class="total-line">
                {{ i18n.t('totalPrice') }}:
                <strong class="accent-text">{{ b.totalPrice }} {{ i18n.t('currency') }}</strong>
              </div>

              @if (b.status === 0 || b.status === 1) {
                <button
                  type="button"
                  class="btn-danger"
                  [disabled]="cancelling()"
                  (click)="cancelCurrentBooking()"
                >
                  ✕ {{ i18n.t('cancelBookingBtn') }}
                </button>
              }
            </div>
          </div>
        }
      </section>
    </div>
  `,
  styles: [
    `
      .lookup-container {
        max-width: 760px;
        margin: 1.5rem auto;
        padding: 0 1rem;
      }
      .lookup-card {
        background: var(--bg-surface);
        border: 1px solid var(--border-color);
        border-radius: var(--radius-lg);
        padding: 2rem;
        box-shadow: var(--shadow-card);
      }
      .subtitle {
        color: var(--text-muted);
        margin-bottom: 1.5rem;
      }
      .search-row {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
      }
      .ref-input {
        flex: 1;
        min-width: 220px;
        font-size: 1.2rem;
        font-weight: 700;
        text-transform: uppercase;
      }
      .result-box {
        background: var(--bg-elevated);
        border: 1px solid var(--border-color);
        border-radius: var(--radius-md);
        padding: 1.5rem;
        margin-top: 1.5rem;
      }
      .result-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        border-bottom: 1px solid var(--border-color);
        padding-bottom: 1rem;
        margin-bottom: 1rem;
      }
      .ref-tag {
        color: var(--brand-primary);
        font-weight: 800;
        font-size: 0.95rem;
      }
      .result-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .lbl {
        display: block;
        font-size: 0.82rem;
        color: var(--text-muted);
      }
      .items-list {
        list-style: none;
        padding: 0;
        margin: 1rem 0;
        border-top: 1px dashed var(--border-color);
      }
      .items-list li {
        display: flex;
        justify-content: space-between;
        padding: 0.6rem 0;
        border-bottom: 1px dashed var(--border-color);
      }
      .result-footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: 1rem;
      }
      .total-line {
        font-size: 1.25rem;
      }
    `,
  ],
})
export class BookingLookupComponent {
  readonly api = inject(ApiService);
  readonly i18n = inject(I18nService);

  readonly referenceCode = signal<string>('');
  readonly loading = signal<boolean>(false);
  readonly cancelling = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly booking = signal<BookingDetailsDto | null>(null);

  searchBooking(): void {
    const code = this.referenceCode().trim();
    if (!code) return;

    this.loading.set(true);
    this.errorMessage.set(null);
    this.booking.set(null);

    this.api.getBookingByReference(code).subscribe({
      next: (data) => {
        this.loading.set(false);
        this.booking.set(data);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.message || 'الحجز غير موجود.');
      },
    });
  }

  cancelCurrentBooking(): void {
    const cur = this.booking();
    if (!cur) return;

    this.cancelling.set(true);
    this.api.cancelBooking(cur.reference).subscribe({
      next: (updated) => {
        this.cancelling.set(false);
        this.booking.set(updated);
      },
      error: (err) => {
        this.cancelling.set(false);
        this.errorMessage.set(err?.error?.message || 'تعذر إلغاء الحجز.');
      },
    });
  }
}

