import type { DXColumnTypeValue } from '../dx-column-type';

/** One column of a DX element, with the lookups needed to render its values. */
export interface UnitColumn {
  readonly name: string;
  readonly type: DXColumnTypeValue | number;
  /** Integer value → label, for enum-backed columns. */
  readonly enumValues: Readonly<Record<string, string>> | null;
  /** Related unit id → its display name, for relation columns. */
  readonly relationValues: Readonly<Record<string, string>> | null;
  /** `false` makes the field required in the editor. */
  readonly allowNull: boolean;
  /** Maximum length for text columns, when the metadata declares one. */
  readonly length: number | null;
  /** Metadata default, as text — DX writes booleans as `'0'` / `'false'`. */
  readonly defaultValue: string | null;
}

export interface UnitElement {
  readonly name: string;
  readonly columns: readonly UnitColumn[];
}

/** `DXModelDefinition`: the shape of one DX unit type. */
export interface UnitStructure {
  readonly name: string;
  readonly main: UnitElement;
  readonly requiredSingle: readonly UnitElement[];
  readonly optionalSingle: readonly UnitElement[];
  readonly requiredMulti: readonly UnitElement[];
  readonly optionalMulti: readonly UnitElement[];
}
