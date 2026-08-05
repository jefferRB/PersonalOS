import {
  buildTimeZoneOptions,
  detectBrowserTimeZone,
  listSupportedTimeZones,
} from './browser-time-zones';

describe('detectBrowserTimeZone', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('reads the zone the browser resolves', () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockReturnValue({
      resolvedOptions: () => ({ timeZone: 'America/Costa_Rica' }),
    } as unknown as Intl.DateTimeFormat);

    expect(detectBrowserTimeZone()).toBe('America/Costa_Rica');
  });

  it('returns null when the browser reports nothing usable', () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockReturnValue({
      resolvedOptions: () => ({ timeZone: '' }),
    } as unknown as Intl.DateTimeFormat);

    expect(detectBrowserTimeZone()).toBeNull();
  });

  it('returns null instead of throwing when detection fails', () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(() => {
      throw new Error('unavailable');
    });

    expect(detectBrowserTimeZone()).toBeNull();
  });
});

describe('listSupportedTimeZones', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('uses the browser list when it is available', () => {
    const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };
    vi.spyOn(intl, 'supportedValuesOf').mockReturnValue(['UTC', 'Asia/Tokyo']);

    expect(listSupportedTimeZones()).toEqual(['UTC', 'Asia/Tokyo']);
  });

  it('falls back to a curated list when the browser API throws', () => {
    const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };
    vi.spyOn(intl, 'supportedValuesOf').mockImplementation(() => {
      throw new Error('unsupported');
    });

    const zones = listSupportedTimeZones();

    expect(zones.length).toBeGreaterThan(0);
    expect(zones).toContain('UTC');
    expect(zones).toContain('America/Costa_Rica');
  });

  it('falls back to a curated list when the browser API returns nothing', () => {
    const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };
    vi.spyOn(intl, 'supportedValuesOf').mockReturnValue([]);

    expect(listSupportedTimeZones()).toContain('America/Costa_Rica');
  });
});

describe('buildTimeZoneOptions', () => {
  it('always offers UTC', () => {
    expect(buildTimeZoneOptions(null, null)).toContain('UTC');
  });

  it('keeps the saved zone even when the browser list omits it', () => {
    const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };
    vi.spyOn(intl, 'supportedValuesOf').mockReturnValue(['UTC']);

    expect(buildTimeZoneOptions('America/Costa_Rica', null)).toContain('America/Costa_Rica');

    vi.restoreAllMocks();
  });

  it('includes the browser suggestion', () => {
    expect(buildTimeZoneOptions(null, 'Europe/Madrid')).toContain('Europe/Madrid');
  });

  it('does not repeat an entry that appears twice', () => {
    const options = buildTimeZoneOptions('Asia/Tokyo', 'Asia/Tokyo');
    const occurrences = options.filter((option) => option === 'Asia/Tokyo');

    expect(occurrences).toHaveLength(1);
  });

  it('sorts the options predictably', () => {
    const options = [...buildTimeZoneOptions('America/Costa_Rica', 'Europe/Madrid')];
    const sorted = [...options].sort((left, right) => left.localeCompare(right, 'en'));

    expect(options).toEqual(sorted);
  });
});
