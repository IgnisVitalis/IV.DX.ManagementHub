/** `DXPDataSetViewUnit`: the metadata describing one table screen. */
export interface DatasetViewDefinition {
  readonly id: string;
  /** Heading shown above the table. */
  readonly title: string;
  /** Id of the DX query that produces the rows. */
  readonly queryId: string;
  readonly isCreatable: boolean;
  readonly isEditable: boolean;
  readonly isDeletable: boolean;
  readonly isExportable: boolean;
}
