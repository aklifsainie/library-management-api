Help to create all endpoint as below

## 1. Book Endpoints

| Method   | Endpoint          | Purpose                 |
| -------- | ----------------- | ----------------------- |
| `POST`   | `/api/books`      | Add a book              |
| `GET`    | `/api/books`      | List books              |
| `GET`    | `/api/books/{id}` | Get a book’s details    |
| `PUT`    | `/api/books/{id}` | Update book information |
| `DELETE` | `/api/books/{id}` | Delete a book           |

---

## 2. Member Endpoints

- Please link this as well with the Identity user table for the borrowed book.. like for the particular book, who borrow it? State the UserId from the IdentityTable.

| Method | Endpoint                 | Purpose                  |
| ------ | ------------------------ | ------------------------ |
| `POST` | `/api/loans`             | Borrow a book            |
| `GET`  | `/api/loans`             | List borrowing records   |
| `GET`  | `/api/loans/{id}`        | Get a borrowing record   |
| `POST` | `/api/loans/{id}/return` | Return the borrowed book |
