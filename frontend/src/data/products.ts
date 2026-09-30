import type { ProductDTO } from "../dto/product.dto";

export const products: ProductDTO[] = [
  {
    id: 1,
    name: "Action Figure Homem-Aranha",
    category: "MARVEL",
    price: 249.9,
    image:
      "https://i.pinimg.com/1200x/0f/20/9c/0f209c09a2fbb01b5d6ace36203f091a.jpg",
    description: "Action figure colecionável do Homem-Aranha.",
    favorite: false,
  },
  {
    id: 2,
    name: "Katana Demon Slayer",
    category: "ANIME",
    price: 349.9,
    image:
      "https://i.pinimg.com/736x/e7/da/67/e7da67678004dbb111a67219cf6fbb11.jpg",
    description: "Réplica colecionável inspirada em Demon Slayer.",
    favorite: false,
  },
  {
    id: 3,
    name: "Figure edição limitada",
    category: "GAMES",
    price: 189.9,
    image:
      "https://i.pinimg.com/736x/04/4c/2c/044c2ca67b47dbc156872f89300948b4.jpg",
    description: "Figure de edição limitada para colecionadores.",
    favorite: false,
  },
  {
    id: 4,
    name: "Réplica colecionável",
    category: "FANTASIA",
    price: 299.9,
    image:
      "https://i.pinimg.com/1200x/6d/b3/19/6db31995c8ebcdd320aa99ca9566eccc.jpg",
    description: "Réplica para colecionadores.",
    favorite: false,
  },
];
