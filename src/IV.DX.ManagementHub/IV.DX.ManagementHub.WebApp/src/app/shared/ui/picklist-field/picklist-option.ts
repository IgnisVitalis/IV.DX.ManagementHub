/** One choice of a picklist. */
export interface PicklistOption {
  readonly value: string;
  readonly label: string;
}

export interface PicklistDialogData {
  readonly title: string;
  readonly options: readonly PicklistOption[];
  readonly value: string | null;
  /** Adds an explicit "clear" entry. */
  readonly allowNull: boolean;
}

/** Closing without this result means the user cancelled. */
export interface PicklistResult {
  readonly value: string | null;
}

/**
 * Options matching the search text.
 *
 * Matching is on the label only: the value is an opaque identifier the user
 * never sees, so letting it match would produce results that look unrelated.
 */
export function filterOptions(
  options: readonly PicklistOption[],
  search: string,
): readonly PicklistOption[] {
  const needle = search.trim().toLocaleLowerCase();

  if (needle === '') {
    return options;
  }

  return options.filter((option) => option.label.toLocaleLowerCase().includes(needle));
}
