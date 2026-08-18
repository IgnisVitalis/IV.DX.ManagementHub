/**
 * Wire contract of the DX unit endpoints (`GET /api/i/{instanceKey}/{TypeName}/{id}`).
 *
 * A single unit still comes back wrapped in the multi-item envelope, so reading
 * one means taking the first item.
 */
export interface DXDataBlock<TItem> {
  readonly Meta: { readonly Type: string };
  readonly Data: { readonly Items: readonly TItem[] | null };
}

function isDXDataBlock(value: unknown): value is DXDataBlock<unknown> {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const items = (value as DXDataBlock<unknown>).Data?.Items;

  return items === null || Array.isArray(items);
}

/** Items of a data block, or an empty list when the payload is not one. */
export function dataBlockItems<TItem>(value: unknown): readonly TItem[] {
  return isDXDataBlock(value) ? ((value.Data.Items ?? []) as readonly TItem[]) : [];
}

/** First item of a data block, or `null` when there is none. */
export function firstDataBlockItem<TItem>(value: unknown): TItem | null {
  return dataBlockItems<TItem>(value)[0] ?? null;
}
