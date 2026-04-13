import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

/**
 * shadcn/ui utility — merges Tailwind class names intelligently,
 * resolving conflicts (e.g. `p-4` vs `p-2` correctly keeps last).
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs))
}
