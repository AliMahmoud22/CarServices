import { Routes } from '@angular/router';
import { AdminDashboardComponent } from './features/admin/admin-dashboard.component';
import { BookingWizardComponent } from './features/booking/booking-wizard.component';
import { BookingLookupComponent } from './features/lookup/booking-lookup.component';

export const routes: Routes = [
  { path: '', component: BookingWizardComponent },
  { path: 'lookup', component: BookingLookupComponent },
  { path: 'admin', component: AdminDashboardComponent },
  { path: '**', redirectTo: '' },
];
