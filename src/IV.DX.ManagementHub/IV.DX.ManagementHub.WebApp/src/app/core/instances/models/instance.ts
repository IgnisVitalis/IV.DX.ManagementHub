/** One DX instance the hub can work against (`MHInstanceUnit`). */
export interface Instance {
  readonly id: string;
  /** Key used in URLs and in the instance-scoped API prefix. */
  readonly key: string;
  /** Human-readable label. */
  readonly title: string;
}
