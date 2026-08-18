import { firstDataBlockItem } from '@core/api/models/dx-data-block';
import { isSystemColumn, isSystemColumnDefinitionRow } from './dx-column-type';
import { EMPTY_VALUE, formatValue } from './format-value';
import type { PreviewField, PreviewGroup, UnitPreview } from './models/unit-preview';
import type { UnitElement, UnitStructure } from './models/unit-structure';

type RecordValues = Readonly<Record<string, unknown>>;

/** Wire shape of one record: main columns inline, sub-elements under DXElements. */
interface UnitRecordPayload extends RecordValues {
  readonly DXElements?: Readonly<
    Record<string, { readonly Data?: { readonly Items?: readonly RecordValues[] | null } }>
  > | null;
}

function elementItems(record: UnitRecordPayload | null, name: string): readonly RecordValues[] {
  const items = record?.DXElements?.[name]?.Data?.Items ?? [];

  return items.filter((item) => !isSystemColumnDefinitionRow(name, item['Name']));
}

/** Columns a reader actually cares about. */
function visibleColumns(element: UnitElement) {
  return element.columns.filter((column) => !isSystemColumn(column.name));
}

function toFields(element: UnitElement, values: RecordValues | null): readonly PreviewField[] {
  return visibleColumns(element).map((column) => ({
    name: column.name,
    value: values === null ? EMPTY_VALUE : formatValue(column, values[column.name] ?? null),
  }));
}

function singleGroup(
  element: UnitElement,
  values: RecordValues | null,
  expanded: boolean,
): PreviewGroup {
  return {
    name: element.name,
    kind: 'single',
    expanded,
    fields: toFields(element, values),
    columns: [],
    rows: [],
    isEmpty: values === null,
  };
}

function multiGroup(
  element: UnitElement,
  items: readonly RecordValues[],
  expanded: boolean,
): PreviewGroup {
  const columns = visibleColumns(element);

  return {
    name: element.name,
    kind: 'multi',
    expanded,
    fields: [],
    columns: columns.map((column) => column.name),
    rows: items.map((item) =>
      Object.fromEntries(
        columns.map(
          (column) => [column.name, formatValue(column, item[column.name] ?? null)] as const,
        ),
      ),
    ),
    isEmpty: items.length === 0,
  };
}

/**
 * Builds the preview from a record and the structure of its type.
 *
 * Ordering follows the Blazor details view: the main element first, then the
 * required elements, then the optional ones. Required groups start expanded
 * because they always carry data; optional ones start collapsed so a unit with
 * many empty elements still opens on something readable.
 */
export function toUnitPreview(structure: UnitStructure | null, rawRecord: unknown): UnitPreview {
  if (structure === null) {
    return { typeName: '', groups: [] };
  }

  const record = firstDataBlockItem<UnitRecordPayload>(rawRecord);
  const groups: PreviewGroup[] = [singleGroup(structure.main, record, true)];

  for (const element of structure.requiredSingle) {
    groups.push(singleGroup(element, elementItems(record, element.name)[0] ?? null, true));
  }

  for (const element of structure.requiredMulti) {
    groups.push(multiGroup(element, elementItems(record, element.name), true));
  }

  for (const element of structure.optionalSingle) {
    groups.push(singleGroup(element, elementItems(record, element.name)[0] ?? null, false));
  }

  for (const element of structure.optionalMulti) {
    groups.push(multiGroup(element, elementItems(record, element.name), false));
  }

  return { typeName: structure.name, groups };
}
