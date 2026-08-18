/** `DXPCardViewUnit`: metadata of one card screen. */
export interface CardViewDefinition {
  readonly id: string;
  readonly title: string;
  /** Unit definition whose records are shown as cards. */
  readonly unitDefinitionId: string;
  readonly isCreatable: boolean;
  readonly isEditable: boolean;
  readonly isDeletable: boolean;
  readonly isExportable: boolean;
}

export interface CardItem {
  readonly id: string;
  /** `DXTitle` of the record, falling back to its identifier. */
  readonly title: string;
}

export interface CardSet {
  /** DX unit type of the records, needed to act on them. */
  readonly typeName: string;
  readonly items: readonly CardItem[];
}

export const EMPTY_CARD_SET: CardSet = { typeName: '', items: [] };
