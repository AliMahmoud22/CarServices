import { Injectable, effect, signal } from '@angular/core';

export type ColorMode = 'light' | 'dark';
export type BrandPalette = 'amber' | 'cobalt' | 'emerald' | 'crimson';

export interface PaletteOption {
  id: BrandPalette;
  labelAr: string;
  labelEn: string;
  previewColor: string;
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly colorMode = signal<ColorMode>(
    (localStorage.getItem('cs_color_mode') as ColorMode) || 'dark'
  );

  readonly palette = signal<BrandPalette>(
    (localStorage.getItem('cs_palette') as BrandPalette) || 'amber'
  );

  readonly kioskMode = signal<boolean>(
    localStorage.getItem('cs_kiosk') === 'true'
  );

  readonly palettes: PaletteOption[] = [
    { id: 'amber', labelAr: 'ذهبي رياضي', labelEn: 'Motorsport Amber', previewColor: '#f59e0b' },
    { id: 'cobalt', labelAr: 'أزرق كهربائي', labelEn: 'Electric Cobalt', previewColor: '#3b82f6' },
    { id: 'emerald', labelAr: 'أخضر زمردي', labelEn: 'Emerald Garage', previewColor: '#10b981' },
    { id: 'crimson', labelAr: 'أحمر سباقات', labelEn: 'Racing Crimson', previewColor: '#ef4444' },
  ];

  constructor() {
    effect(() => {
      const mode = this.colorMode();
      const pal = this.palette();
      const kiosk = this.kioskMode();

      localStorage.setItem('cs_color_mode', mode);
      localStorage.setItem('cs_palette', pal);
      localStorage.setItem('cs_kiosk', String(kiosk));

      document.documentElement.setAttribute('data-theme', mode);
      document.documentElement.setAttribute('data-palette', pal);
      document.documentElement.setAttribute('data-kiosk', String(kiosk));
    });
  }

  toggleColorMode(): void {
    this.colorMode.set(this.colorMode() === 'dark' ? 'light' : 'dark');
  }

  setPalette(palette: BrandPalette): void {
    this.palette.set(palette);
  }

  toggleKioskMode(): void {
    const next = !this.kioskMode();
    this.kioskMode.set(next);
    if (next && document.documentElement.requestFullscreen) {
      document.documentElement.requestFullscreen().catch(() => {});
    } else if (!next && document.fullscreenElement) {
      document.exitFullscreen().catch(() => {});
    }
  }
}

