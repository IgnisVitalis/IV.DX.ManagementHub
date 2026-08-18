import { dataBlockItems, firstDataBlockItem } from '@core/api/models/dx-data-block';
import {
  EMPTY_CARD_SET,
  type CardItem,
  type CardSet,
  type CardViewDefinition,
} from './models/card-view';

/** Wire shape of a `DXPCardViewUnit`. */
interface CardViewRow {
  readonly Id: string;
  readonly Name: string | null;
  readonly DXTitle: string | null;
  readonly DXUnitDefinition: string | null;
  readonly IsCreatable: boolean | null;
  readonly IsEditable: boolean | null;
  readonly IsDeletable: boolean | null;
  readonly IsExportable: boolean | null;
}

interface CardRecord {
  readonly Id: string;
  readonly DXTitle?: string | null;
}

/**
 * Maps a `DXPCardViewUnit` payload onto the view definition.
 *
 * Returns `null` when the unit is missing or names no unit definition — without
 * one there are no records to show, and that has to surface as an error rather
 * than as an empty screen.
 */
export function toCardViewDefinition(raw: unknown): CardViewDefinition | null {
  const unit = firstDataBlockItem<CardViewRow>(raw);

  if (unit === null || !unit.DXUnitDefinition) {
    return null;
  }

  return {
    id: unit.Id,
    title: unit.Name ?? unit.DXTitle ?? '',
    unitDefinitionId: unit.DXUnitDefinition,
    isCreatable: unit.IsCreatable === true,
    isEditable: unit.IsEditable === true,
    isDeletable: unit.IsDeletable === true,
    isExportable: unit.IsExportable === true,
  };
}

/** Type name of a data block, or an empty string when the payload is not one. */
function blockTypeName(raw: unknown): string {
  if (typeof raw !== 'object' || raw === null) {
    return '';
  }

  const type = (raw as { Meta?: { Type?: unknown } }).Meta?.Type;

  return typeof type === 'string' ? type : '';
}

/**
 * Maps the records of a unit definition onto cards.
 *
 * A card shows `DXTitle` — the human-readable label DX computes per type. When a
 * record has none, the identifier is shown so the card stays actionable.
 */
export function toCardSet(raw: unknown): CardSet {
  const typeName = blockTypeName(raw);

  if (typeName === '') {
    return EMPTY_CARD_SET;
  }

  const items: readonly CardItem[] = dataBlockItems<CardRecord>(raw).map((record) => ({
    id: record.Id,
    title: record.DXTitle?.trim() ? record.DXTitle : record.Id,
  }));

  return { typeName, items };
}
