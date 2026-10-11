Help to create all endpoint as below

## 1. Loan Endpoints

- Please link this as well with the Identity user table for the borrowed book.. like for the particular book, who borrow it? State the UserId from the IdentityTable.

| Method | Endpoint                 | Purpose                  |
| ------ | ------------------------ | ------------------------ |
| `POST` | `/api/v1/loans`             | Borrow a book            |
| `GET`  | `/api/v1/loans`             | List borrowing records   |
| `GET`  | `/api/v1/loans/{id}`        | Get a borrowing record   |
| `POST` | `/api/v1/loans/{id}/return` | Return the borrowed book |
