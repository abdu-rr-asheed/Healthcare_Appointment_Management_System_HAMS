const STORAGE_PREFIX = 'hams_';

export class StorageService {
  private prefix: string;

  constructor(prefix: string = STORAGE_PREFIX) {
    this.prefix = prefix;
  }

  private getKey(key: string): string {
    return `${this.prefix}${key}`;
  }

  set<T>(key: string, value: T): void {
    try {
      const serialized = JSON.stringify(value);
      localStorage.setItem(this.getKey(key), serialized);
    } catch (error) {
      console.error('Error saving to localStorage:', error);
    }
  }

  get<T>(key: string, defaultValue?: T): T | undefined {
    try {
      const item = localStorage.getItem(this.getKey(key));
      if (item === null) return defaultValue;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error('Error reading from localStorage:', error);
      return defaultValue;
    }
  }

  remove(key: string): void {
    localStorage.removeItem(this.getKey(key));
  }

  clear(): void {
    const keys = Object.keys(localStorage).filter(key => key.startsWith(this.prefix));
    keys.forEach(key => localStorage.removeItem(key));
  }

  has(key: string): boolean {
    return localStorage.getItem(this.getKey(key)) !== null;
  }

  setSession<T>(key: string, value: T): void {
    try {
      const serialized = JSON.stringify(value);
      sessionStorage.setItem(this.getKey(key), serialized);
    } catch (error) {
      console.error('Error saving to sessionStorage:', error);
    }
  }

  getSession<T>(key: string, defaultValue?: T): T | undefined {
    try {
      const item = sessionStorage.getItem(this.getKey(key));
      if (item === null) return defaultValue;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error('Error reading from sessionStorage:', error);
      return defaultValue;
    }
  }

  removeSession(key: string): void {
    sessionStorage.removeItem(this.getKey(key));
  }

  clearSession(): void {
    const keys = Object.keys(sessionStorage).filter(key => key.startsWith(this.prefix));
    keys.forEach(key => sessionStorage.removeItem(key));
  }
}

export const storage = new StorageService();