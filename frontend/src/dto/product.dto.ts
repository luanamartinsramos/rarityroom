export interface ProductDTO {
  id: number;
  name: string;
  category: string;
  price: number;
  image: string;
  description?: string;
  favorite?: boolean;
}
