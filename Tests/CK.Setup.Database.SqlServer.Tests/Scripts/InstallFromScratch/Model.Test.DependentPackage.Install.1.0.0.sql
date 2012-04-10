
create table Test.tTestFromDependentModel
(
	Id int not null,
	Name varchar(23) not null,
	constraint PK_tTestFromAnotherModel primary key (Id)
);


