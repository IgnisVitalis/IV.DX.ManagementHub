import type { UnitColumn, UnitElement, UnitStructure } from './models/unit-structure';

/** Wire shape of `GET /api/i/{instanceKey}/DXUnitStructure/{typeName}`. */
interface UnitColumnPayload {
  readonly Name: string;
  readonly ColumnType: number;
  readonly EnumValues: Record<string, string> | null;
  readonly RelationValues: Record<string, string> | null;
  readonly AllowNull?: boolean | null;
  readonly Length?: number | null;
  readonly DefaultValue?: string | null;
}

interface UnitElementPayload {
  readonly Name: string;
  readonly Columns: readonly UnitColumnPayload[] | null;
}

interface UnitStructurePayload {
  readonly Name: string;
  readonly MainSingleElement: UnitElementPayload | null;
  readonly RequiredSingleElements: readonly UnitElementPayload[] | null;
  readonly OptionalSingleElements: readonly UnitElementPayload[] | null;
  readonly RequiredMultiElements: readonly UnitElementPayload[] | null;
  readonly OptionalMultiElements: readonly UnitElementPayload[] | null;
}

function toColumn(payload: UnitColumnPayload): UnitColumn {
  return {
    name: payload.Name,
    type: payload.ColumnType,
    enumValues: payload.EnumValues ?? null,
    relationValues: payload.RelationValues ?? null,
    allowNull: payload.AllowNull !== false,
    length: payload.Length ?? null,
    defaultValue: payload.DefaultValue ?? null,
  };
}

function toElement(payload: UnitElementPayload): UnitElement {
  return { name: payload.Name, columns: (payload.Columns ?? []).map(toColumn) };
}

/** Elements without columns render nothing, so they never reach the view. */
function toElements(payloads: readonly UnitElementPayload[] | null): readonly UnitElement[] {
  return (payloads ?? []).map(toElement).filter((element) => element.columns.length > 0);
}

function isStructurePayload(value: unknown): value is UnitStructurePayload {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as UnitStructurePayload).MainSingleElement === 'object'
  );
}

/** Maps the unit-structure payload, or `null` when the payload is not one. */
export function toUnitStructure(raw: unknown): UnitStructure | null {
  if (!isStructurePayload(raw) || raw.MainSingleElement === null) {
    return null;
  }

  return {
    name: raw.Name,
    main: toElement(raw.MainSingleElement),
    requiredSingle: toElements(raw.RequiredSingleElements),
    optionalSingle: toElements(raw.OptionalSingleElements),
    requiredMulti: toElements(raw.RequiredMultiElements),
    optionalMulti: toElements(raw.OptionalMultiElements),
  };
}
