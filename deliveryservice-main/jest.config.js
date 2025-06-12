/** @type {import('ts-jest').JestConfigWithTsJest} **/
export default {
  testEnvironment: "node",
  roots: ["<rootDir>/src", "<rootDir>/tests"],
  transform: {
    "^.+\.tsx?$": ["ts-jest",{}],
  },
  coveragePathIgnorePatterns: [
    "<rootDir>/src/models/",
  ],
};