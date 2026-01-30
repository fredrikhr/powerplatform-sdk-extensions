import js from "@eslint/js";
import { defineConfig } from "eslint/config";
import nodePlugin from "eslint-plugin-n";
import prettier from "eslint-plugin-prettier/recommended";

export default [
  ...defineConfig([
    {
      files: ["**/*.{js,mjs,cjs}"],
      plugins: { js, n: nodePlugin },
      extends: ["js/recommended", "n/flat/recommended"],
    },
  ]),
  prettier,
];
